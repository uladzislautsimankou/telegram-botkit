using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.InlineQueries.Middlewares;

/// <summary>
/// Default implementation of the inline query answer middleware.
/// Can be inherited or overridden to customize how results are delivered to Telegram.
/// </summary>
public class DefaultInlineQueryAnswerMiddleware(
    ITelegramBotClient botClient,
    ILogger<DefaultInlineQueryAnswerMiddleware> logger) : IInlineQueryAnswerMiddleware
{
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
    /// Override this method to customize delivery criteria (e.g. skip empty queries).
    /// </summary>
    protected virtual bool ShouldSendAnswer(InlineQueryContext context)
    {
        if (context.SkipAnswer)
        {
            return false;
        }

        return context.IsHandled || context.Results.Count > 0;
    }

    /// <summary>
    /// Executes the Telegram API call. Override this to transform results, inject metrics, etc.
    /// </summary>
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
