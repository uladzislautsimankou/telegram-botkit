using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.InlineQueries.Middlewares;

/// <summary>
/// Default implementation of the inline query answer middleware.
/// Can be inherited or overridden to customize how results are delivered to Telegram.
/// </summary>
/// <param name="botClient">The Telegram bot client used to send the answer.</param>
/// <param name="logger">The logger used to report a failed answer.</param>
public class DefaultInlineQueryAnswerMiddleware(
    ITelegramBotClient botClient,
    ILogger<DefaultInlineQueryAnswerMiddleware> logger) : IInlineQueryAnswerMiddleware
{
    /// <inheritdoc />
    public virtual async ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (ShouldSendAnswer(context))
        {
            await SendAnswerAsync(context, cancellationToken);
        }

        await next();
    }

    /// <summary>
    /// Determines whether the answer should be sent to Telegram.
    /// By default every inline query is answered unless <see cref="InlineQueryContext.SkipAnswer"/> is set,
    /// including a query no handler matched: Telegram expects an answer to each inline query, and an empty one makes
    /// the client show "no results" instead of waiting for a reply that never comes.
    /// Override this method to customize delivery criteria.
    /// </summary>
    /// <param name="context">The context of the inline query with the aggregated results.</param>
    /// <returns>True if the answer should be sent; otherwise, false.</returns>
    protected virtual bool ShouldSendAnswer(InlineQueryContext context) => !context.SkipAnswer;

    /// <summary>
    /// Executes the Telegram API call. Override this to transform results, inject metrics, etc.
    /// </summary>
    /// <param name="context">The context of the inline query with the aggregated results.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    protected virtual async Task SendAnswerAsync(InlineQueryContext context, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.AnswerInlineQuery(
                inlineQueryId: context.Query.Id,
                results: context.Results,
                isPersonal: context.IsPersonal,
                cacheTime: context.CacheTime,
                nextOffset: context.NextOffset,
                button: context.Button,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send inline query answer for query {InlineQueryId}", context.Query.Id);
            throw;
        }
    }
}
