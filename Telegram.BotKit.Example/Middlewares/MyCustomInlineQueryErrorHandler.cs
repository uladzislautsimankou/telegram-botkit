using Telegram.Bot;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Handles framework routing and execution errors for inline queries.
/// Prevents logging unrecognized queries as critical errors and stops the Telegram loading spinner.
/// </summary>
internal sealed class MyCustomInlineQueryErrorHandler(
    ITelegramBotClient bot,
    ILogger<MyCustomInlineQueryErrorHandler> logger
) : IInlineQueryMiddleware
{
    public async ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        //catch (RouteNotFoundException)
        //{
        //    // user typed an unmatched query or just opened the inline input.
        //    // we answer with empty results to stop the perpetual loading spinner in Telegram.
        //    await bot.AnswerInlineQuery(
        //        inlineQueryId: context.Query.Id,
        //        results: Array.Empty<InlineQueryResult>(),
        //        cancellationToken: cancellationToken
        //    );
        //}
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error handling inline query");
        }
    }
}
