using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// Execution middleware responsible for running the matched message handler.
/// </summary>
internal sealed class MessageExecutionMiddleware : IMessageMiddleware
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedMessageMetadata is not { } metadata)
        {
            return ValueTask.CompletedTask;
        }

        // the handler is created here, only when the pipeline actually reaches execution
        var handler = (IMessageHandler)metadata.HandlerInstance;
        return new ValueTask(handler.HandleAsync(context, cancellationToken));
    }
}
