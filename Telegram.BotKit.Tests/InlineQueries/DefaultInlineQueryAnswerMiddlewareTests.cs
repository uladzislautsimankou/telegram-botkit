using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Middlewares;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class DefaultInlineQueryAnswerMiddlewareTests
{
    private readonly ITelegramBotClient _botClient = Substitute.For<ITelegramBotClient>();
    private readonly ILogger<DefaultInlineQueryAnswerMiddleware> _logger = Substitute.For<ILogger<DefaultInlineQueryAnswerMiddleware>>();

    public DefaultInlineQueryAnswerMiddlewareTests()
    {
        _botClient.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
    }

    // verifies that handled query sends telegram response with all aggregated parameters
    [Fact]
    public async Task InvokeAsync_WhenContextIsHandled_ShouldSendAnswerInlineQueryWithAllParameters()
    {
        // arrange
        var middleware = new DefaultInlineQueryAnswerMiddleware(_botClient, _logger);
        var context = CreateContext("search");
        context.IsHandled = true;
        context.AddResults([Substitute.For<InlineQueryResult>()]);
        context.MergeConfiguration(
            isPersonal: true,
            cacheTime: 600,
            nextOffset: "page_2",
            button: new InlineQueryResultsButton("Click me") { StartParameter = "start" });

        var nextCalled = false;
        NextDelegate next = () => { nextCalled = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        nextCalled.Should().BeTrue();
        await _botClient.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(req => IsExpectedAnswerRequest(req)),
            TestContext.Current.CancellationToken);
    }

    // verifies that skip answer flag prevents sending response to telegram api
    [Fact]
    public async Task InvokeAsync_WhenSkipAnswerIsTrue_ShouldNotCallBotClient()
    {
        // arrange
        var middleware = new DefaultInlineQueryAnswerMiddleware(_botClient, _logger);
        var context = CreateContext("search");
        context.IsHandled = true;
        context.SkipAnswer = true;

        var nextCalled = false;
        NextDelegate next = () => { nextCalled = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        nextCalled.Should().BeTrue();
        await _botClient.DidNotReceiveWithAnyArgs().SendRequest(
            Arg.Any<IRequest<bool>>(),
            Arg.Any<CancellationToken>());
    }

    // verifies that unhandled query with empty results skips sending response
    [Fact]
    public async Task InvokeAsync_WhenNotHandledAndResultsEmpty_ShouldNotCallBotClient()
    {
        // arrange
        var middleware = new DefaultInlineQueryAnswerMiddleware(_botClient, _logger);
        var context = CreateContext("unknown");
        context.IsHandled = false;

        var nextCalled = false;
        NextDelegate next = () => { nextCalled = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        nextCalled.Should().BeTrue();
        await _botClient.DidNotReceiveWithAnyArgs().SendRequest(
            Arg.Any<IRequest<bool>>(),
            Arg.Any<CancellationToken>());
    }

    // verifies that client exception is logged and rethrown
    [Fact]
    public async Task InvokeAsync_WhenBotClientThrows_ShouldLogErrorAndRethrow()
    {
        // arrange
        _botClient.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new HttpRequestException("Telegram API network timeout")));

        var middleware = new DefaultInlineQueryAnswerMiddleware(_botClient, _logger);
        var context = CreateContext("search");
        context.IsHandled = true;

        NextDelegate next = () => ValueTask.CompletedTask;

        // act
        var act = () => middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken).AsTask();

        // assert
        await act.Should().ThrowAsync<HttpRequestException>();

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to send inline query answer")),
            Arg.Is<Exception>(ex => ex is HttpRequestException),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static bool IsExpectedAnswerRequest(IRequest<bool> req) =>
        req is AnswerInlineQueryRequest request
        && request.InlineQueryId == "query_123"
        && request.Results.Count() == 1
        && request.IsPersonal == true
        && request.CacheTime == 600
        && request.NextOffset == "page_2"
        && request.Button is not null
        && request.Button.Text == "Click me";

    private static InlineQueryContext CreateContext(string queryText) =>
        new(new InlineQuery
        {
            Id = "query_123",
            Query = queryText,
            From = new User { Id = 456, FirstName = "User" }
        });
}