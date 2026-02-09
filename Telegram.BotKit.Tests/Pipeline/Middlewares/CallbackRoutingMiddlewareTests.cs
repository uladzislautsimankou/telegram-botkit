using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Invocation;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public partial class CallbackRoutingMiddlewareTests
{
    private readonly MockCallbackHandler _handler = new("menu:main");

    [Fact] // просто ключ
    public async Task Should_Execute_Callback_When_Key_Matches_Exactly()
    {
        var binder = new MockCallbackParameterBinder();
        var invoker = new CallbackHandlerInvoker<object>(_handler, binder);
        var middleware = new CallbackRoutingMiddleware([invoker]);

        var context = CreateCallbackContext("menu:main");
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.True(_handler.WasExecuted);
        Assert.False(called);
    }

    [Fact] // ключ с параметрами
    public async Task Should_Execute_Callback_When_Key_Matches_With_Params()
    {
        var binder = new MockCallbackParameterBinder();
        var invoker = new CallbackHandlerInvoker<object>(_handler, binder);
        var middleware = new CallbackRoutingMiddleware([invoker]);

        var context = CreateCallbackContext("menu:main?id=123");

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        Assert.True(_handler.WasExecuted);
    }

    [Fact] // хендлер не найден
    public async Task Should_Throw_RouteNotFoundException_For_Unknown_Callback()
    {
        var binder = new MockCallbackParameterBinder();
        var invoker = new CallbackHandlerInvoker<object>(_handler, binder);
        var middleware = new CallbackRoutingMiddleware([invoker]);

        var context = CreateCallbackContext("unknown:action");

        await Assert.ThrowsAsync<RouteNotFoundException>(
            () => middleware.InvokeAsync(context, () => Task.CompletedTask));
    }
}
