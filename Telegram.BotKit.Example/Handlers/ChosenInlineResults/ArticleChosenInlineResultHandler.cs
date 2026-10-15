using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;

namespace Telegram.BotKit.Example.Handlers.ChosenInlineResults;

/// <summary>
/// Handles telemetry tracking when a user chooses an article inline search result.
/// </summary>
internal sealed class ArticleChosenInlineResultHandler(
    ILogger<ArticleChosenInlineResultHandler> logger
) : IChosenInlineResultHandler
{
    public static bool CanHandle(ChosenInlineResult chosenResult) =>
        chosenResult.ResultId.StartsWith("article_", StringComparison.OrdinalIgnoreCase);

    public Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("User {UserId} selected article inline result: {ResultId}",
            context.Result.From.Id,
            context.Result.ResultId);

        return Task.CompletedTask;
    }
}
