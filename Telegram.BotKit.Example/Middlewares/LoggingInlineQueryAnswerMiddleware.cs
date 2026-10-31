using Telegram.Bot;
using Telegram.BotKit.InlineQueries.Middlewares;
using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Custom inline query answer middleware demonstrating result inspection and logging before dispatch.
/// </summary>
internal sealed class LoggingInlineQueryAnswerMiddleware(
    ITelegramBotClient botClient,
    ILogger<LoggingInlineQueryAnswerMiddleware> logger
) : DefaultInlineQueryAnswerMiddleware(botClient, logger)
{
    protected override async Task SendAnswerAsync(InlineQueryContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending inline answer with {ResultCount} results for query '{Query}'",
            context.Results.Count,
            context.Query.Query);

        await base.SendAnswerAsync(context, cancellationToken);
    }
}
