using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Invocation;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal class InlineQueryRoutingMiddleware(
    IEnumerable<IInlineQueryHandlerInvoker> invokers
    ) : IInlineQueryMiddleware
{
    private readonly Dictionary<string, IInlineQueryHandlerInvoker> _handlerMap
        = invokers.ToDictionary(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (!_handlerMap.TryGetValue(context.Key, out var invoker))
        {
            //коллбэк не нашли
            throw new RouteNotFoundException(context.Key);
        }

        await invoker.InvokeAsync(context, cancellationToken);
    }
}
