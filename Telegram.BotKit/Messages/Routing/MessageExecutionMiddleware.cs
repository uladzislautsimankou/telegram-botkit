using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// Execution middleware responsible for running the matched message handler.
/// </summary>
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