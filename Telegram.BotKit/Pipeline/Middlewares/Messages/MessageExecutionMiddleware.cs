using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class MessageExecutionMiddleware : IMessageMiddleware
{
    public async Task InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedHandler is not null)
        {
            await context.MatchedHandler.HandleAsync(context, cancellationToken);
        }
    }
}