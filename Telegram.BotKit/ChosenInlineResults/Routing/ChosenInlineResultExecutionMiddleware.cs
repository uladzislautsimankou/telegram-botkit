using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.ChosenInlineResults.Routing;

/// <summary>
/// Execution middleware responsible for running the matched chosen inline result handler.
/// </summary>
internal sealed class ChosenInlineResultExecutionMiddleware : IChosenInlineResultMiddleware
{
    public async Task InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedHandler is not null)
        {
            await context.MatchedHandler.HandleAsync(context, cancellationToken);
        }
    }
}