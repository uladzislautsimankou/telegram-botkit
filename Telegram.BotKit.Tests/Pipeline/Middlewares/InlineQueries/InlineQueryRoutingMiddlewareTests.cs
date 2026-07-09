using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Invocation;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public sealed class InlineQueryRoutingMiddlewareTests
{
    // verifies that an inline query starting with a registered key trigger is routed to and executed by its handler
    [Fact]
    public async Task InvokeAsync_WhenQueryStartsWithRegisteredKey_ShouldExecuteHandlerAndShortCircuit()
    {
        // arrange
        var invoker = Substitute.For<IInlineQueryHandlerInvoker>();
        invoker.Key.Returns("search");
        var middleware = new InlineQueryRoutingMiddleware([invoker]);
        var context = CreateInlineContext("search items");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        await invoker.Received(1).InvokeAsync(context, TestContext.Current.CancellationToken);
        called.Should().BeFalse();
    }

    // verifies that an inline query matching a registered key trigger exactly is matched and executed by the handler
    [Fact]
    public async Task InvokeAsync_WhenQueryMatchesKeyExactly_ShouldExecuteHandlerAndShortCircuit()
    {
        // arrange
        var invoker = Substitute.For<IInlineQueryHandlerInvoker>();
        invoker.Key.Returns("search");
        var middleware = new InlineQueryRoutingMiddleware([invoker]);
        var context = CreateInlineContext("search");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        await invoker.Received(1).InvokeAsync(context, TestContext.Current.CancellationToken);
        called.Should().BeFalse();
    }

    // verifies that an unregistered inline query trigger throws a route not found exception
    [Fact]
    public async Task InvokeAsync_WhenQueryKeyIsUnknown_ShouldThrowRouteNotFoundException()
    {
        // arrange
        var invoker = Substitute.For<IInlineQueryHandlerInvoker>();
        invoker.Key.Returns("search");
        var middleware = new InlineQueryRoutingMiddleware([invoker]);
        var context = CreateInlineContext("wiki something");

        // act
        var act = () => middleware.InvokeAsync(context, () => Task.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<RouteNotFoundException>();
    }

    // verifies that an empty inline query fails to resolve and throws a route not found exception
    [Fact]
    public async Task InvokeAsync_WhenQueryIsEmpty_ShouldThrowRouteNotFoundException()
    {
        // arrange
        var invoker = Substitute.For<IInlineQueryHandlerInvoker>();
        invoker.Key.Returns("search");
        var middleware = new InlineQueryRoutingMiddleware([invoker]);
        var context = CreateInlineContext("");

        // act
        var act = () => middleware.InvokeAsync(context, () => Task.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<RouteNotFoundException>();
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