using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Invocation;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public partial class InlineQueryRoutingMiddlewareTests
{
    private readonly MockInlineQueryHandler _handler = new("search");

    [Fact] // ключ с аргументами
    public async Task Should_Execute_Handler_When_Query_Starts_With_Key()
    {
        var binder = new MockInlineQueryParameterBinder();
        var invoker = new InlineQueryHandlerInvoker<object>(_handler, binder);
        var middleware = new InlineQueryRoutingMiddleware([invoker]);

        var context = CreateInlineContext("search items");

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        Assert.True(_handler.WasExecuted);
    }

    [Fact] // ключ без аргументов
    public async Task Should_Execute_Handler_When_Query_Is_Exactly_Key()
    {
        var binder = new MockInlineQueryParameterBinder();
        var invoker = new InlineQueryHandlerInvoker<object>(_handler, binder);
        var middleware = new InlineQueryRoutingMiddleware([invoker]);

        var context = CreateInlineContext("search");

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        Assert.True(_handler.WasExecuted);
    }

    [Fact] // хендлер не найден
    public async Task Should_Throw_RouteNotFoundException_When_No_Handler_Matches()
    {
        var binder = new MockInlineQueryParameterBinder();
        var invoker = new InlineQueryHandlerInvoker<object>(_handler, binder);
        var middleware = new InlineQueryRoutingMiddleware([invoker]);

        var context = CreateInlineContext("wiki something");

        await Assert.ThrowsAsync<RouteNotFoundException>(
            () => middleware.InvokeAsync(context, () => Task.CompletedTask));
    }

    [Fact] // пустой запрос
    public async Task Should_Throw_RouteNotFoundException_On_Empty_Query()
    {
        var binder = new MockInlineQueryParameterBinder();
        var invoker = new InlineQueryHandlerInvoker<object>(_handler, binder);
        var middleware = new InlineQueryRoutingMiddleware([invoker]);

        var context = CreateInlineContext("");

        await Assert.ThrowsAsync<RouteNotFoundException>(
            () => middleware.InvokeAsync(context, () => Task.CompletedTask));
    }
}
