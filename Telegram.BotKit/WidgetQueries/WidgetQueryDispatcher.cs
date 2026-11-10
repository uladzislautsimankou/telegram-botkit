using Telegram.Bot.Types;
using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.WidgetQueries;

/// <summary>
/// Dispatches widget queries by executing them through the compiled middleware pipeline.
/// </summary>
/// <param name="serviceProvider">The root service provider used to create a dependency scope for each widget query.</param>
/// <param name="pipeline">The compiled widget query middleware pipeline.</param>
internal sealed class WidgetQueryDispatcher(IServiceProvider serviceProvider, PipelineDelegate<WidgetQueryContext> pipeline)
    : DispatcherBase<WidgetQueryContext>(serviceProvider, pipeline), IWidgetQueryDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(WidgetQuery query, CancellationToken cancellationToken = default) =>
        RunAsync(new WidgetQueryContext(query), cancellationToken);

    /// <inheritdoc />
    public Task DispatchAsync(SharedWidgetQuery query, CancellationToken cancellationToken = default) =>
        RunAsync(new WidgetQueryContext(query), cancellationToken);
}
