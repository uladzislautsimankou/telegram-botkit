using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.ChosenInlineResults.Routing;

/// <summary>
/// Execution middleware responsible for running the matched chosen inline result handler.
/// </summary>
internal sealed class ChosenInlineResultExecutionMiddleware : IChosenInlineResultMiddleware
{
    public ValueTask InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedHandler is null)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(context.MatchedHandler.HandleAsync(context, cancellationToken));
    }
}
