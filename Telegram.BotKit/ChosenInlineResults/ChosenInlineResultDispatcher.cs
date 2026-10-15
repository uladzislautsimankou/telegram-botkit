using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.ChosenInlineResults;

/// <summary>
/// Dispatches incoming chosen inline result updates by executing them through the compiled pipeline.
/// </summary>
internal sealed class ChosenInlineResultDispatcher(
    IServiceProvider serviceProvider,
    PipelineDelegate<ChosenInlineResultContext> pipeline) : IChosenInlineResultDispatcher
{
    public async Task DispatchAsync(ChosenInlineResult chosenResult, CancellationToken cancellationToken = default)
    {
        var context = new ChosenInlineResultContext(chosenResult);

        // create a dedicated async dependency scope for the chosen inline result processing
        await using var scope = serviceProvider.CreateAsyncScope();

        // execute the precompiled middleware delegate chain
        await pipeline(context, scope.ServiceProvider, cancellationToken);
    }
}
