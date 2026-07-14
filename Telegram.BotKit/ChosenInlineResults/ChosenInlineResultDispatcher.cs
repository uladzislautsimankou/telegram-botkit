using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;

namespace Telegram.BotKit.ChosenInlineResults;

/// <summary>
/// Dispatches incoming chosen inline result updates by executing them through the configured pipeline.
/// </summary>
internal sealed class ChosenInlineResultDispatcher(
    IServiceProvider serviceProvider,
    ChosenInlineResultPipelineBuilder pipelineBuilder
    ) : IChosenInlineResultDispatcher
{
    public async Task DispatchAsync(ChosenInlineResult chosenResult, CancellationToken cancellationToken = default)
    {
        var context = new ChosenInlineResultContext(chosenResult);

        using var scope = serviceProvider.CreateScope();

        var steps = pipelineBuilder.Steps;
        var resolvedMiddlewares = new IChosenInlineResultMiddleware[steps.Count];

        for (var i = 0; i < steps.Count; i++)
        {
            resolvedMiddlewares[i] = (IChosenInlineResultMiddleware)scope.ServiceProvider.GetRequiredService(steps[i].MiddlewareType);
        }

        var runner = new PipelineRunner(resolvedMiddlewares, context, cancellationToken);
        await runner.NextAsync();
    }

    private sealed class PipelineRunner(
        IChosenInlineResultMiddleware[] middlewares,
        ChosenInlineResultContext context,
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