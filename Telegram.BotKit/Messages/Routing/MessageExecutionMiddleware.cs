using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// Execution middleware responsible for running the matched message handler.
/// </summary>
internal sealed class MessageExecutionMiddleware : IMessageMiddleware
{
    public ValueTask InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedHandler is null)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(context.MatchedHandler.HandleAsync(context, cancellationToken));
    }
}
