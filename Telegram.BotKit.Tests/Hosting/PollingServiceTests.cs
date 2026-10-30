using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Hosting.Background;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Hosting;

public sealed class PollingServiceTests : IDisposable
{
    private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
    private readonly IUpdateHandler _updateHandler = Substitute.For<IUpdateHandler>();
    private readonly ILogger<PollingService> _logger = Substitute.For<ILogger<PollingService>>();
    private readonly BotInfo _botInfo = new();
    private readonly List<GetUpdatesRequest> _pollRequests = [];
    private readonly CancellationTokenSource _stop = new(TimeSpan.FromSeconds(10));

    public PollingServiceTests()
    {
        _bot.SendRequest(Arg.Any<IRequest<User>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new User { Id = 1, FirstName = "Bot", Username = "test_bot", IsBot = true }));
        _bot.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _bot.SendRequest(Arg.Any<IRequest<Update[]>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (call.ArgAt<object>(0) is GetUpdatesRequest request)
                {
                    _pollRequests.Add(request);
                }

                _stop.Cancel();
                return Task.FromResult(Array.Empty<Update>());
            });
    }

    public void Dispose() => _stop.Dispose();

    private PollingService CreateService(Action<TelegramBotOptions>? configure = null)
    {
        var options = new TelegramBotOptions { Token = "123456:ABC" };
        configure?.Invoke(options);

        return new PollingService(_bot, _botInfo, _updateHandler, Options.Create(options), _logger);
    }

    private async Task RunAsync(PollingService service)
    {
        try
        {
            await service.StartAsync(_stop.Token);

            if (service.ExecuteTask is { } task)
            {
                await task;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    // verifies that a bot that cannot be identified (wrong token, no network) stops the host with a clear exception
    [Fact]
    public async Task ExecuteAsync_WhenIdentityRequestFails_ShouldThrowBotStartupExceptionAndNotPoll()
    {
        _bot.SendRequest(Arg.Any<IRequest<User>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<User>(new HttpRequestException("network down")));
        var service = CreateService();

        var act = () => RunAsync(service);

        await act.Should().ThrowAsync<BotStartupException>();
        _pollRequests.Should().BeEmpty();
        await _bot.DidNotReceiveWithAnyArgs().SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>());
    }

    // verifies that the identity of the bot is stored before polling starts (routing needs the username)
    [Fact]
    public async Task ExecuteAsync_WhenBotIsIdentified_ShouldStoreBotInfo()
    {
        await RunAsync(CreateService());

        _botInfo.Username.Should().Be("test_bot");
        _botInfo.Id.Should().Be(1);
    }

    // verifies that a failing webhook deletion does not prevent polling, it is only reported
    [Fact]
    public async Task ExecuteAsync_WhenWebhookDeletionFails_ShouldWarnAndKeepPolling()
    {
        _bot.SendRequest(Arg.Is<IRequest<bool>>(r => r is DeleteWebhookRequest), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new HttpRequestException("conflict")));

        await RunAsync(CreateService());

        _pollRequests.Should().NotBeEmpty();
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("webhook")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that an existing webhook is removed before polling, and that pending updates are dropped only on request
    [Fact]
    public async Task ExecuteAsync_WhenDropPendingUpdatesIsSet_ShouldPassItToDeleteWebhook()
    {
        await RunAsync(CreateService(o => o.DropPendingUpdates = true));

        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(r => r is DeleteWebhookRequest && ((DeleteWebhookRequest)r).DropPendingUpdates == true),
            Arg.Any<CancellationToken>());
    }

    // verifies that pending updates are kept by default
    [Fact]
    public async Task ExecuteAsync_WhenDropPendingUpdatesIsNotSet_ShouldKeepPendingUpdates()
    {
        await RunAsync(CreateService());

        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(r => r is DeleteWebhookRequest && ((DeleteWebhookRequest)r).DropPendingUpdates != true),
            Arg.Any<CancellationToken>());
    }

    // verifies that only the configured update types are requested from Telegram
    [Fact]
    public async Task ExecuteAsync_WhenAllowedUpdatesAreConfigured_ShouldRequestOnlyThem()
    {
        await RunAsync(CreateService(o => o.AllowedUpdates = ["Message", "callbackquery"]));

        _pollRequests.Should().NotBeEmpty();
        _pollRequests.Should().OnlyContain(r => r.AllowedUpdates != null
            && r.AllowedUpdates.SequenceEqual(new[] { UpdateType.Message, UpdateType.CallbackQuery }));
    }
}
