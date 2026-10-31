using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.ChosenInlineResults.Routing;

/// <summary>
/// Execution middleware responsible for running the matched chosen inline result handler.
/// </summary>
internal sealed class ChosenInlineResultExecutionMiddleware : IChosenInlineResultMiddleware
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedChosenInlineResultMetadata is not { } metadata)
        {
            return ValueTask.CompletedTask;
        }

        var handler = (IChosenInlineResultHandler)metadata.HandlerInstance;
        return new ValueTask(handler.HandleAsync(context, cancellationToken));
    }
}
