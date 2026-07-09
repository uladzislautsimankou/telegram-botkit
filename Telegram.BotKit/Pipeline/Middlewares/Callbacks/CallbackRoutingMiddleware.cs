using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Invocation;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class CallbackRoutingMiddleware(
    IEnumerable<ICallbackHandlerInvoker> invokers
    ) : ICallbackMiddleware
{
    private readonly Dictionary<string, ICallbackHandlerInvoker> _handlerMap
        = invokers.ToDictionary(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (!_handlerMap.TryGetValue(context.Key, out var invoker))
        {
            // callback handler not found
            throw new RouteNotFoundException(context.Key);
        }

        await invoker.InvokeAsync(context, cancellationToken);

        // do not invoke next() as this is the end of the pipeline
    }
}
