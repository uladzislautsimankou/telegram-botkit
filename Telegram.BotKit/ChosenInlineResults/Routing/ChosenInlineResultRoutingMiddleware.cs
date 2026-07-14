using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.ChosenInlineResults.Routing;

/// <summary>
/// Routing middleware responsible for identifying the appropriate chosen inline result handler.
/// </summary>
internal sealed class ChosenInlineResultRoutingMiddleware(
    IEnumerable<IChosenInlineResultHandler> handlers) : IChosenInlineResultMiddleware
{
    public async Task InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        foreach (var handler in handlers)
        {
            if (handler.CanHandle(context.Result))
            {
                context.MatchedHandler = handler;
                break;
            }
        }

        await next();
    }
}