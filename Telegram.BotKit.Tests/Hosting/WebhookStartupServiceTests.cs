using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Hosting.Webhook;

namespace Telegram.BotKit.Tests.Hosting;

public sealed class WebhookStartupServiceTests
{
    private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
    private readonly ILogger<WebhookStartupService> _logger = Substitute.For<ILogger<WebhookStartupService>>();

    public WebhookStartupServiceTests()
    {
        _bot.SendRequest(Arg.Any<IRequest<User>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new User { Id = 1, FirstName = "Bot", Username = "test_bot", IsBot = true }));
        _bot.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
    }

    private WebhookStartupService CreateService(string? secretToken = "secret", bool deleteOnShutdown = false) =>
        new(_bot, new BotInfo(), Options.Create(new TelegramBotOptions
        {
            Token = "123456:ABC",
            Mode = BotMode.Webhook,
            WebhookUrl = "https://example.com/api/bot",
            SecretToken = secretToken,
            DeleteWebhookOnShutdown = deleteOnShutdown
        }), _logger);

    // verifies that the webhook stays registered on shutdown by default
    [Fact]
    public async Task StopAsync_WhenDeleteOnShutdownIsDisabled_ShouldNotCallTelegram()
    {
        var service = CreateService(deleteOnShutdown: false);

        await service.StopAsync(TestContext.Current.CancellationToken);

        await _bot.DidNotReceiveWithAnyArgs().SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>());
    }

    // verifies that the webhook is removed on shutdown when explicitly requested
    [Fact]
    public async Task StopAsync_WhenDeleteOnShutdownIsEnabled_ShouldDeleteWebhook()
    {
        var service = CreateService(deleteOnShutdown: true);

        await service.StopAsync(TestContext.Current.CancellationToken);

        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(r => r is DeleteWebhookRequest),
            Arg.Any<CancellationToken>());
    }

    // verifies that a failing webhook deletion never breaks application shutdown
    [Fact]
    public async Task StopAsync_WhenDeletionFails_ShouldNotThrow()
    {
        _bot.SendRequest(Arg.Is<IRequest<bool>>(r => r is DeleteWebhookRequest), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new HttpRequestException("network down")));
        var service = CreateService(deleteOnShutdown: true);

        var act = () => service.StopAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    // verifies that the webhook is registered with the configured url and secret
    [Fact]
    public async Task StartAsync_WhenCalled_ShouldRegisterWebhookWithUrlAndSecret()
    {
        var service = CreateService(secretToken: "secret");

        await service.StartAsync(TestContext.Current.CancellationToken);

        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(r => r is SetWebhookRequest
                                        && ((SetWebhookRequest)r).Url == "https://example.com/api/bot"
                                        && ((SetWebhookRequest)r).SecretToken == "secret"),
            Arg.Any<CancellationToken>());
    }

    // verifies that a missing secret token produces a warning and a configured one does not
    [Theory]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData("secret", 0)]
    public async Task StartAsync_WhenSecretTokenIsMissing_ShouldWarn(string? secret, int expectedWarnings)
    {
        var service = CreateService(secretToken: secret);

        await service.StartAsync(TestContext.Current.CancellationToken);

        _logger.Received(expectedWarnings).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("SecretToken")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
