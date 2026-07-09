using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Invocation;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public sealed class CallbackRoutingMiddlewareTests
{
    // verifies that a callback with an exact key match executes the matched handler and short-circuits the pipeline
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatchesExactly_ShouldExecuteHandlerAndShortCircuit()
    {
        // arrange
        var invoker = Substitute.For<ICallbackHandlerInvoker>();
        invoker.Key.Returns("menu:main");
        var middleware = new CallbackRoutingMiddleware([invoker]);
        var context = CreateCallbackContext("menu:main");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        await invoker.Received(1).InvokeAsync(context, TestContext.Current.CancellationToken);
        called.Should().BeFalse();
    }

    // verifies that a callback key containing parameters is matched and processed by the corresponding handler
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatchesWithParameters_ShouldExecuteHandler()
    {
        // arrange
        var invoker = Substitute.For<ICallbackHandlerInvoker>();
        invoker.Key.Returns("menu:main");
        var middleware = new CallbackRoutingMiddleware([invoker]);
        var context = CreateCallbackContext("menu:main?id=123");

        // act
        await middleware.InvokeAsync(context, () => Task.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        await invoker.Received(1).InvokeAsync(context, TestContext.Current.CancellationToken);
    }

    // verifies that an unknown callback trigger throws a route not found exception
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyIsUnknown_ShouldThrowRouteNotFoundException()
    {
        // arrange
        var invoker = Substitute.For<ICallbackHandlerInvoker>();
        invoker.Key.Returns("menu:main");
        var middleware = new CallbackRoutingMiddleware([invoker]);
        var context = CreateCallbackContext("unknown:action");

        // act
        var act = () => middleware.InvokeAsync(context, () => Task.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<RouteNotFoundException>();
    }

    private CallbackContext CreateCallbackContext(string data)
    {
        var query = new CallbackQuery
        {
            Id = "1",
            Data = data,
            From = new User { Id = 123, FirstName = "User" },
            Message = new Message { Chat = new Chat { Id = 456 } }
        };

        return new CallbackContext(query);
    }
}