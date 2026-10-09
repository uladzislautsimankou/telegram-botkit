using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.InlineQueries.Routing;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class InlineQueryRoutingMiddlewareTests
{
    private readonly ILogger<InlineQueryRoutingMiddleware> _logger = Substitute.For<ILogger<InlineQueryRoutingMiddleware>>();

    // verifies that when no registered inline handlers can handle the query, the next pipeline delegate is called and results are empty
    [Fact]
    public async Task InvokeAsync_WhenNoHandlersCanHandle_ShouldCallNextAndNotPopulateResults()
    {
        // arrange
        var handler = Substitute.For<IInlineQueryHandler>();
        handler.CanHandle(Arg.Any<InlineQuery>()).Returns(false);

        var middleware = new InlineQueryRoutingMiddleware([handler], _logger);
        var context = CreateInlineContext("search query");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.Results.Should().BeEmpty();
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<InlineQueryContext>(), Arg.Any<CancellationToken>());
    }

    // verifies that when a single handler can handle the query, it is executed directly, populating context properties
    [Fact]
    public async Task InvokeAsync_WhenSingleHandlerMatches_ShouldInvokeHandlerAndPopulateResultsAndCallNext()
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
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.Results.Should().ContainSingle().Which.Should().BeSameAs(result);
        context.IsPersonal.Should().BeTrue();
        context.CacheTime.Should().Be(300);
        context.NextOffset.Should().Be("offset_val");
        await handler.Received(1).HandleAsync(context, TestContext.Current.CancellationToken);
    }

    // verifies that when multiple handlers can handle the query, they are executed and their results are aggregated in the context
    [Fact]
    public async Task InvokeAsync_WhenMultipleHandlersMatch_ShouldInvokeAllAndAggregateResultsAndCallNext()
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
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.Results.Should().HaveCount(2);
        context.Results.Should().Contain([result1, result2]);
        context.IsPersonal.Should().BeTrue();
        context.CacheTime.Should().Be(300);
        context.NextOffset.Should().Be("offset2");
    }

    // verifies that when a handler throws an exception, it is caught and stored as a HandlerFailedException in the context
    [Fact]
    public async Task InvokeAsync_WhenHandlerThrows_ShouldCaptureExceptionAndCallNext()
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
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context
            .Exceptions.Should().ContainSingle()
            .Which.Should().BeOfType<HandlerFailedException>()
            .Which.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("DB error");
    }

    // verifies that when a single handler returns a button, it is successfully populated in the context
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
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.Button.Should().BeSameAs(button);
    }

    // verifies that when multiple handlers provide a button, the last executed one overwrites and a warning is logged
    [Fact]
    public async Task InvokeAsync_WhenMultipleHandlersProvideButton_ShouldKeepLastAndLogWarning()
    {
        // arrange
        var result1 = Substitute.For<InlineQueryResult>();
        var button1 = new InlineQueryResultsButton("Button 1") { StartParameter = "param1" };
        var batch1 = new InlineQueryBatch([result1], button: button1);

        var result2 = Substitute.For<InlineQueryResult>();
        var button2 = new InlineQueryResultsButton("Button 2") { StartParameter = "param2" };
        var batch2 = new InlineQueryBatch([result2], button: button2);

        var handler1 = Substitute.For<IInlineQueryHandler>();
        handler1.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        handler1.HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken).Returns(batch1);

        var handler2 = Substitute.For<IInlineQueryHandler>();
        handler2.CanHandle(Arg.Any<InlineQuery>()).Returns(true);
        handler2.HandleAsync(Arg.Any<InlineQueryContext>(), TestContext.Current.CancellationToken).Returns(batch2);

        var middleware = new InlineQueryRoutingMiddleware([handler1, handler2], _logger);
        var context = CreateInlineContext("search query");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.Button.Should().BeSameAs(button2);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Conflict: Multiple inline query handlers")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private InlineQueryContext CreateInlineContext(string queryText)
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