using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.ChosenInlineResults;

/// <summary>
/// Dispatches incoming chosen inline result updates by executing them through the compiled middleware pipeline.
/// </summary>
/// <param name="serviceProvider">The root service provider used to create a dependency scope for each update.</param>
/// <param name="pipeline">The compiled chosen inline result middleware pipeline.</param>
internal sealed class ChosenInlineResultDispatcher(IServiceProvider serviceProvider, PipelineDelegate<ChosenInlineResultContext> pipeline)
    : DispatcherBase<ChosenInlineResultContext>(serviceProvider, pipeline), IChosenInlineResultDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(ChosenInlineResult chosenResult, CancellationToken cancellationToken = default) =>
        RunAsync(new ChosenInlineResultContext(chosenResult), cancellationToken);
}
