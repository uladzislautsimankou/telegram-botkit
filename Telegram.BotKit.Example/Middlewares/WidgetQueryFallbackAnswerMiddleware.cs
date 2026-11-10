using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Placed before <c>UseRouting()</c>, so it sees the end of the whole pipeline: no handler matched the key
/// (typically a widget sent before a deployment renamed or removed its handler) or the handler failed.
/// The framework never answers widget queries, and a widget that is not answered waits for 15 seconds
/// and gets a timeout, so the middleware ends the wait with an empty answer.
/// </summary>
internal sealed class WidgetQueryFallbackAnswerMiddleware(
    ITelegramBotClient bot,
    ILogger<WidgetQueryFallbackAnswerMiddleware> logger
) : IWidgetQueryMiddleware
{
    public async ValueTask InvokeAsync(WidgetQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();

            // a handler ran and answered
            if (context.MatchedWidgetQueryMetadata is not null)
            {
                return;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Widget query '{Key}' failed", context.Key);
        }

        try
        {
            await bot.AnswerWidgetQuery(widgetQueryId: context.Id, cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            // the handler may have answered before it failed, or the query is too old
            logger.LogDebug(ex, "The fallback answer to widget query '{Key}' was not accepted", context.Key);
        }
    }
}
