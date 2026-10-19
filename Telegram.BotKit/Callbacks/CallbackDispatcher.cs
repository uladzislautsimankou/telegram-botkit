using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Callbacks;

/// <summary>
/// Dispatches callback queries by executing them through the compiled middleware pipeline.
/// </summary>
/// <param name="serviceProvider">The root service provider used to create a dependency scope for each callback query.</param>
/// <param name="pipeline">The compiled callback middleware pipeline.</param>
internal sealed class CallbackDispatcher(IServiceProvider serviceProvider, PipelineDelegate<CallbackContext> pipeline)
    : DispatcherBase<CallbackContext>(serviceProvider, pipeline), ICallbackDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(CallbackQuery query, CancellationToken cancellationToken = default) =>
        RunAsync(new CallbackContext(query), cancellationToken);
}
