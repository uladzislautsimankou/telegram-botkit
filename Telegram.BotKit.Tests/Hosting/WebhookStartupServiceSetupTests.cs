using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Hosting.Webhook;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Hosting;

public sealed class WebhookStartupServiceSetupTests
{
    private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
    private readonly ILogger<WebhookStartupService> _logger = Substitute.For<ILogger<WebhookStartupService>>();
    private readonly BotInfo _botInfo = new();

    public WebhookStartupServiceSetupTests()
    {
        _bot.SendRequest(Arg.Any<IRequest<User>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new User { Id = 1, FirstName = "Bot", Username = "test_bot", IsBot = true }));
        _bot.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
    }

    private WebhookStartupService CreateService(Action<TelegramBotOptions>? configure = null)
    {
        var options = new TelegramBotOptions
        {
            Token = "123456:ABC",
            Mode = BotMode.Webhook,
            WebhookUrl = "https://example.com/api/bot",
            SecretToken = "secret"
        };
        configure?.Invoke(options);

        return new WebhookStartupService(_bot, _botInfo, Options.Create(options), _logger);
    }

    // verifies that the optional settings reach Telegram in the registration request
    [Fact]
    public async Task StartAsync_WhenOptionalSettingsAreSet_ShouldPassThemToSetWebhook()
    {
        var service = CreateService(o =>
        {
            o.IpAddress = "203.0.113.5";
            o.MaxConnections = 40;
            o.DropPendingUpdates = true;
            o.AllowedUpdates = ["Message", "InlineQuery"];
        });

        await service.StartAsync(TestContext.Current.CancellationToken);

        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(r => r is SetWebhookRequest
                && ((SetWebhookRequest)r).IpAddress == "203.0.113.5"
                && ((SetWebhookRequest)r).MaxConnections == 40
                && ((SetWebhookRequest)r).DropPendingUpdates == true
                && ((SetWebhookRequest)r).AllowedUpdates != null
                && ((SetWebhookRequest)r).AllowedUpdates!.SequenceEqual(new[] { UpdateType.Message, UpdateType.InlineQuery })),
            Arg.Any<CancellationToken>());
    }

    // verifies that the identity of the bot is stored: routing needs the username to recognize /command@bot
    [Fact]
    public async Task StartAsync_WhenCalled_ShouldStoreBotInfo()
    {
        var service = CreateService();

        await service.StartAsync(TestContext.Current.CancellationToken);

        _botInfo.Username.Should().Be("test_bot");
    }

    // verifies that a rejected registration (e.g. a URL Telegram refuses) stops the application with a clear exception
    [Fact]
    public async Task StartAsync_WhenSetWebhookFails_ShouldThrowBotStartupException()
    {
        _bot.SendRequest(Arg.Is<IRequest<bool>>(r => r is SetWebhookRequest), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new HttpRequestException("bad url")));
        var service = CreateService();

        var act = () => service.StartAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BotStartupException>()).WithInnerException<HttpRequestException>();
    }

    // verifies that a bot that cannot be identified does not get a webhook registered
    [Fact]
    public async Task StartAsync_WhenIdentityRequestFails_ShouldThrowBotStartupExceptionWithoutRegisteringWebhook()
    {
        _bot.SendRequest(Arg.Any<IRequest<User>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<User>(new HttpRequestException("network down")));
        var service = CreateService();

        var act = () => service.StartAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BotStartupException>();
        await _bot.DidNotReceive().SendRequest(
            Arg.Is<IRequest<bool>>(r => r is SetWebhookRequest),
            Arg.Any<CancellationToken>());
    }

    // verifies that an existing certificate file is sent to Telegram
    [Fact]
    public async Task StartAsync_WhenCertificateFileExists_ShouldSendCertificate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pem");
        await File.WriteAllTextAsync(path, "certificate", TestContext.Current.CancellationToken);

        try
        {
            var service = CreateService(o => o.CertificatePath = path);

            await service.StartAsync(TestContext.Current.CancellationToken);

            await _bot.Received(1).SendRequest(
                Arg.Is<IRequest<bool>>(r => r is SetWebhookRequest && ((SetWebhookRequest)r).Certificate != null),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(path);
        }
    }

    // verifies that a missing certificate file is reported, but does not prevent the registration
    [Fact]
    public async Task StartAsync_WhenCertificateFileIsMissing_ShouldWarnAndRegisterWithoutCertificate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pem");
        var service = CreateService(o => o.CertificatePath = path);

        await service.StartAsync(TestContext.Current.CancellationToken);

        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(r => r is SetWebhookRequest && ((SetWebhookRequest)r).Certificate == null),
            Arg.Any<CancellationToken>());
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Certificate file not found")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
