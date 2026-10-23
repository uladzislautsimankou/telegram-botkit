using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.BotKit.Hosting.Webhook;

namespace Telegram.BotKit.Tests.Hosting;

public sealed class WebhookEndpointTests
{
    private const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";

    // snake_case, as Telegram sends it: the ASP.NET default settings would leave update_id at 0
    private const string ValidUpdate =
        """{"update_id":7,"message":{"message_id":5,"date":1700000000,"chat":{"id":42,"type":"private"},"from":{"id":42,"is_bot":false,"first_name":"Ann"},"text":"hello"}}""";

    private readonly IUpdateHandler _updateHandler = Substitute.For<IUpdateHandler>();
    private readonly ITelegramBotClient _botClient = Substitute.For<ITelegramBotClient>();
    private readonly ILogger _logger = Substitute.For<ILogger>();

    private static DefaultHttpContext CreateContext(string body, string? secretHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        if (secretHeader is not null)
        {
            context.Request.Headers[SecretHeader] = secretHeader;
        }

        return context;
    }

    private Task<IResult> HandleAsync(DefaultHttpContext context, string? secretToken = null, CancellationToken cancellationToken = default) =>
        WebhookEndpoint.HandleAsync(context, secretToken, _updateHandler, _botClient, _logger, cancellationToken);

    private static int StatusOf(IResult result) => result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject.StatusCode!.Value;

    private void ReceivedLog(LogLevel level, int times) =>
        _logger.Received(times).Log(
            level,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());

    // verifies that the body is read with Telegram's JSON settings and handed to the update handler
    [Fact]
    public async Task HandleAsync_WhenBodyIsValidUpdate_ShouldDeserializeWithTelegramSettingsAndCallHandler()
    {
        var result = await HandleAsync(CreateContext(ValidUpdate), cancellationToken: TestContext.Current.CancellationToken);

        StatusOf(result).Should().Be(StatusCodes.Status200OK);
        await _updateHandler.Received(1).HandleUpdateAsync(
            _botClient,
            Arg.Is<Update>(u => u.Id == 7 && u.Message != null && u.Message.Text == "hello" && u.Message.Chat.Id == 42),
            Arg.Any<CancellationToken>());
    }

    // verifies that a body that is not an update is rejected and never reaches the handler
    [Theory]
    [InlineData("not json")]
    [InlineData("{\"update_id\":")]
    [InlineData("null")]
    [InlineData("")]
    public async Task HandleAsync_WhenBodyIsNotAnUpdate_ShouldReturnBadRequestWithoutCallingHandler(string body)
    {
        var result = await HandleAsync(CreateContext(body), cancellationToken: TestContext.Current.CancellationToken);

        StatusOf(result).Should().Be(StatusCodes.Status400BadRequest);
        await _updateHandler.DidNotReceiveWithAnyArgs().HandleUpdateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    // verifies that an exception of the handler does not fail the request: Telegram would redeliver the update forever
    [Fact]
    public async Task HandleAsync_WhenHandlerThrows_ShouldLogReportToHandlerAndReturnOk()
    {
        var failure = new InvalidOperationException("boom");
        _updateHandler.HandleUpdateAsync(Arg.Any<ITelegramBotClient>(), Arg.Any<Update>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(failure));

        var result = await HandleAsync(CreateContext(ValidUpdate), cancellationToken: TestContext.Current.CancellationToken);

        StatusOf(result).Should().Be(StatusCodes.Status200OK);
        ReceivedLog(LogLevel.Error, 1);
        await _updateHandler.Received(1).HandleErrorAsync(
            _botClient,
            failure,
            HandleErrorSource.HandleUpdateError,
            Arg.Any<CancellationToken>());
    }

    // verifies that a failing error handler is only logged, so the response is still 200
    [Fact]
    public async Task HandleAsync_WhenHandlerAndErrorHandlerThrow_ShouldLogBothAndReturnOk()
    {
        _updateHandler.HandleUpdateAsync(Arg.Any<ITelegramBotClient>(), Arg.Any<Update>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));
        _updateHandler.HandleErrorAsync(Arg.Any<ITelegramBotClient>(), Arg.Any<Exception>(), Arg.Any<HandleErrorSource>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("error handler failed")));

        var result = await HandleAsync(CreateContext(ValidUpdate), cancellationToken: TestContext.Current.CancellationToken);

        StatusOf(result).Should().Be(StatusCodes.Status200OK);
        ReceivedLog(LogLevel.Error, 2);
    }

    // verifies that cancellation of the request is not swallowed as a handler failure
    [Fact]
    public async Task HandleAsync_WhenRequestIsCancelled_ShouldRethrowWithoutReportingError()
    {
        // the request is aborted while the handler is running
        using var cts = new CancellationTokenSource();
        _updateHandler.HandleUpdateAsync(Arg.Any<ITelegramBotClient>(), Arg.Any<Update>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return Task.FromCanceled(cts.Token);
            });

        var act = () => HandleAsync(CreateContext(ValidUpdate), cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await _updateHandler.DidNotReceiveWithAnyArgs().HandleErrorAsync(default!, default!, default, TestContext.Current.CancellationToken);
    }

    // verifies that the secret token is required and compared exactly when one is configured
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong")]
    [InlineData("SECRET")]
    public async Task HandleAsync_WhenSecretTokenIsWrongOrMissing_ShouldReturnUnauthorizedWithoutCallingHandler(string? header)
    {
        var result = await HandleAsync(CreateContext(ValidUpdate, header), "secret", TestContext.Current.CancellationToken);

        StatusOf(result).Should().Be(StatusCodes.Status401Unauthorized);
        await _updateHandler.DidNotReceiveWithAnyArgs().HandleUpdateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    // verifies that the right secret token lets the update through
    [Fact]
    public async Task HandleAsync_WhenSecretTokenMatches_ShouldCallHandler()
    {
        var result = await HandleAsync(CreateContext(ValidUpdate, "secret"), "secret", TestContext.Current.CancellationToken);

        StatusOf(result).Should().Be(StatusCodes.Status200OK);
        await _updateHandler.Received(1).HandleUpdateAsync(_botClient, Arg.Any<Update>(), Arg.Any<CancellationToken>());
    }

    // verifies that a header is ignored when no secret token is configured
    [Fact]
    public async Task HandleAsync_WhenNoSecretTokenIsConfigured_ShouldIgnoreHeader()
    {
        var result = await HandleAsync(CreateContext(ValidUpdate, "anything"), cancellationToken: TestContext.Current.CancellationToken);

        StatusOf(result).Should().Be(StatusCodes.Status200OK);
    }

    // verifies that the secret token is checked before the body is read
    [Fact]
    public async Task HandleAsync_WhenSecretTokenIsWrong_ShouldNotReadBody()
    {
        var context = CreateContext(ValidUpdate, "wrong");

        await HandleAsync(context, "secret", TestContext.Current.CancellationToken);

        context.Request.Body.Position.Should().Be(0);
    }
}
