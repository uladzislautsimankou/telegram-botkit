using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.InlineQueries;

/// <summary>
/// Dispatches incoming inline query updates by executing them through the compiled middleware pipeline.
/// </summary>
internal sealed class InlineQueryDispatcher(
    ITelegramBotClient botClient,
    IServiceProvider serviceProvider,
    PipelineDelegate<InlineQueryContext> pipeline,
    ILogger<InlineQueryDispatcher> logger) : IInlineQueryDispatcher
{
    public async Task DispatchAsync(InlineQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        using var logScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["InlineQueryId"] = query.Id,
            ["UserId"] = query.From.Id,
            ["QueryText"] = query.Query
        });

        logger.LogInformation("Processing inline query from user {UserId}", query.From.Id);

        var context = new InlineQueryContext(query);

        try
        {
            // execute the precompiled middleware delegate chain
            await pipeline(context, scope.ServiceProvider, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical failure during inline query pipeline execution");
        }

        // send final aggregated response to telegram
        try
        {
            await botClient.AnswerInlineQuery(
                inlineQueryId: query.Id,
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
            logger.LogError(ex, "Failed to send inline query answer for query {InlineQueryId}", query.Id);
        }
    }
}