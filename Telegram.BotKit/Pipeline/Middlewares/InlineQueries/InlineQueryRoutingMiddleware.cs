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
        // attempt to route by the exact first word (e.g., "@bot search item")
        if (_handlerMap.TryGetValue(context.Key, out var invoker))
        {
            await invoker.InvokeAsync(context, cancellationToken);
            return;
        }

        // if no exact route matches the key, fallback to a global handler (without a key)
        if (_handlerMap.TryGetValue(string.Empty, out var fallbackInvoker))
        {
            await fallbackInvoker.InvokeAsync(context, cancellationToken);
            return;
        }

        throw new RouteNotFoundException(context.Key);
    }
}
