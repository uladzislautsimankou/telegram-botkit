using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.InlineQueries.Routing;
using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class InlineQueryRoutingMiddlewareTests
{
    private readonly ILogger<InlineQueryRoutingMiddleware> _logger = Substitute.For<ILogger<InlineQueryRoutingMiddleware>>();

    // verifies that when no handlers match the pipeline continues with ishandled set to false
    [Fact]
    public async Task InvokeAsync_WhenNoHandlersCanHandle_ShouldCallNextAndKeepIsHandledFalse()
    {
        // arrange
        var handler = Substitute.For<IInlineQueryHandler>();
        handler.CanHandle(Arg.Any<InlineQuery>()).Returns(false);

        var middleware = new InlineQueryRoutingMiddleware([handler], _logger);
        var context = CreateInlineContext("search query");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.IsHandled.Should().BeFalse();
        context.Results.Should().BeEmpty();
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<InlineQueryContext>(), Arg.Any<CancellationToken>());
    }

    // verifies that single matching handler executes directly and sets ishandled to true
    [Fact]
    public async Task InvokeAsync_WhenSingleHandlerMatches_ShouldInvokeHandlerAndSetIsHandledTrue()
    {
        // arrange
        var result = Substitute.For<InlineQueryResult>();
        var batch = new InlineQueryBatch([result], isPersonal: true, cacheTime: 300, nextOffset: "offset_val");

        var handler = Substitute.For<IInlineQueryHandler>();
        handler.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        handler.HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken).Returns(batch);

        var middleware = new InlineQueryRoutingMiddleware([handler], _logger);
        var context = CreateInlineContext("search query");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.IsHandled.Should().BeTrue();
        context.Results.Should().ContainSingle().Which.Should().BeSameAs(result);
        context.IsPersonal.Should().BeTrue();
        context.CacheTime.Should().Be(300);
        context.NextOffset.Should().Be("offset_val");
        await handler.Received(1).HandleAsync(context, TestContext.Current.CancellationToken);
    }

    // verifies that multiple matching handlers execute and aggregate results in context
    [Fact]
    public async Task InvokeAsync_WhenMultipleHandlersMatch_ShouldInvokeAllAndSetIsHandledTrue()
    {
        // arrange
        var result1 = Substitute.For<InlineQueryResult>();
        var batch1 = new InlineQueryBatch([result1], isPersonal: false, cacheTime: 600, nextOffset: "offset1");

        var result2 = Substitute.For<InlineQueryResult>();
        var batch2 = new InlineQueryBatch([result2], isPersonal: true, cacheTime: 300, nextOffset: "offset2");

        var handler1 = Substitute.For<IInlineQueryHandler>();
        handler1.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        handler1.HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken).Returns(batch1);

        var handler2 = Substitute.For<IInlineQueryHandler>();
        handler2.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        handler2.HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken).Returns(batch2);

        var middleware = new InlineQueryRoutingMiddleware([handler1, handler2], _logger);
        var context = CreateInlineContext("search query");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.IsHandled.Should().BeTrue();
        context.Results.Should().HaveCount(2);
        context.Results.Should().Contain([result1, result2]);
        context.IsPersonal.Should().BeTrue();
        context.CacheTime.Should().Be(300);
        context.NextOffset.Should().Be("offset2");
    }

    // verifies that failing handler records exception in context without unhandled termination
    [Fact]
    public async Task InvokeAsync_WhenHandlerThrows_ShouldCaptureExceptionLogAndCallNext()
    {
        // arrange
        var handler = Substitute.For<IInlineQueryHandler>();
        handler.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        handler
            .HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromException<InlineQueryBatch>(new InvalidOperationException("DB error")));

        var middleware = new InlineQueryRoutingMiddleware([handler], _logger);
        var context = CreateInlineContext("search query");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.IsHandled.Should().BeTrue();
        context
            .Exceptions.Should().ContainSingle()
            .Which.Should().BeOfType<HandlerFailedException>()
            .Which.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("DB error");

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex is InvalidOperationException && ex.Message == "DB error"),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that single handler failure does not prevent remaining handlers from returning results
    [Fact]
    public async Task InvokeAsync_WhenOneHandlerThrowsAmongMultiple_ShouldCaptureExceptionAndAggregateRemainingResults()
    {
        // arrange
        var successResult = Substitute.For<InlineQueryResult>();
        var successBatch = new InlineQueryBatch([successResult]);

        var failingHandler = Substitute.For<IInlineQueryHandler>();
        failingHandler.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        failingHandler
            .HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromException<InlineQueryBatch>(new InvalidOperationException("API failure")));

        var successHandler = Substitute.For<IInlineQueryHandler>();
        successHandler.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        successHandler
            .HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult(successBatch));

        var middleware = new InlineQueryRoutingMiddleware([failingHandler, successHandler], _logger);
        var context = CreateInlineContext("search query");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.IsHandled.Should().BeTrue();
        context.Results.Should().ContainSingle().Which.Should().BeSameAs(successResult);
        context.Exceptions.Should().ContainSingle().Which.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    // verifies that single handler button is set on context
    [Fact]
    public async Task InvokeAsync_WhenSingleHandlerProvidesButton_ShouldPopulateButtonInContext()
    {
        // arrange
        var result = Substitute.For<InlineQueryResult>();
        var button = new InlineQueryResultsButton("Test Button") { StartParameter = "start_param" };
        var batch = new InlineQueryBatch([result], button: button);

        var handler = Substitute.For<IInlineQueryHandler>();
        handler.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        handler.HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken).Returns(batch);

        var middleware = new InlineQueryRoutingMiddleware([handler], _logger);
        var context = CreateInlineContext("search query");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.Button.Should().BeSameAs(button);
    }

    // verifies that when multiple handlers provide button the last executed overwrites without error
    [Fact]
    public async Task InvokeAsync_WhenMultipleHandlersProvideButton_ShouldKeepLastButtonSilently()
    {
        // arrange
        var button1 = new InlineQueryResultsButton("Button 1") { StartParameter = "param1" };
        var batch1 = new InlineQueryBatch([Substitute.For<InlineQueryResult>()], button: button1);

        var button2 = new InlineQueryResultsButton("Button 2") { StartParameter = "param2" };
        var batch2 = new InlineQueryBatch([Substitute.For<InlineQueryResult>()], button: button2);

        var handler1 = Substitute.For<IInlineQueryHandler>();
        handler1.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        handler1.HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken).Returns(batch1);

        var handler2 = Substitute.For<IInlineQueryHandler>();
        handler2.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        handler2.HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken).Returns(batch2);

        var middleware = new InlineQueryRoutingMiddleware([handler1, handler2], _logger);
        var context = CreateInlineContext("search query");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.Button.Should().BeSameAs(button2);
    }

    private static InlineQueryContext CreateInlineContext(string queryText)
    {
        var query = new InlineQuery
        {
            Id = "1",
            Query = queryText,
            From = new User { Id = 123, FirstName = "User" }
        };

        return new InlineQueryContext(query);
    }
}
