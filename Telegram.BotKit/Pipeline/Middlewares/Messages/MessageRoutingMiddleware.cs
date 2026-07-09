using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class MessageRoutingMiddleware(IEnumerable<IMessageHandler> handlers) : IMessageMiddleware
{
    public async Task InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        IMessageHandler? matched = null;

        // find a handler bound specifically to the current user state
        if (context.UserState is not null)
        {
            matched = handlers.FirstOrDefault(h => h.RequiredState == context.UserState && h.CanHandle(context));
        }

        // if no state-specific handler matches or no state is active, look for a global handler
        if (matched is null)
        {
            matched = handlers.FirstOrDefault(h => h.RequiredState is null && h.CanHandle(context));
        }

        if (matched is not null)
        {
            context.MatchedHandler = matched;
            await next();
        }
    }
}