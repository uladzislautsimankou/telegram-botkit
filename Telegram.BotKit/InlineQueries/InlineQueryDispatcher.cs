using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.InlineQueries;

/// <summary>
/// Dispatches incoming inline query updates by executing them through the configured inline query middleware pipeline.
/// </summary>
internal sealed class InlineQueryDispatcher(
    ITelegramBotClient botClient,
    IServiceProvider serviceProvider,
    InlineQueryPipelineBuilder pipelineBuilder,
    ILogger<InlineQueryDispatcher> logger
    ) : IInlineQueryDispatcher
{
    public async Task DispatchAsync(InlineQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        using var logScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["InlineQueryId"] = query.Id,
            ["UserId"] = query.From.Id,
            ["QueryText"] = query.Query
        });

        logger.LogInformation("Processing inline query from user {UserId}", query.From.Id);

        var context = new InlineQueryContext(query);

        var steps = pipelineBuilder.Steps;
        var resolvedMiddlewares = new IInlineQueryMiddleware[steps.Count];

        for (var i = 0; i < steps.Count; i++)
        {
            resolvedMiddlewares[i] = (IInlineQueryMiddleware)scope.ServiceProvider.GetRequiredService(steps[i].MiddlewareType);
        }

        var runner = new PipelineRunner(resolvedMiddlewares, context, cancellationToken);

        try
        {
            await runner.NextAsync();
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

    private sealed class PipelineRunner(
        IInlineQueryMiddleware[] middlewares,
        InlineQueryContext context,
        CancellationToken cancellationToken)
    {
        private int _index;

        public Task NextAsync()
        {
            if (_index < middlewares.Length)
            {
                var middleware = middlewares[_index++];
                return middleware.InvokeAsync(context, NextAsync, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}