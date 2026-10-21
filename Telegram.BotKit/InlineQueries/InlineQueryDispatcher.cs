using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.InlineQueries;

/// <summary>
/// Dispatches incoming inline query updates by executing them through the compiled middleware pipeline.
/// </summary>
/// <param name="serviceProvider">The root service provider used to create a dependency scope for each update.</param>
/// <param name="pipeline">The compiled inline query middleware pipeline.</param>
internal sealed class InlineQueryDispatcher(IServiceProvider serviceProvider, PipelineDelegate<InlineQueryContext> pipeline)
    : DispatcherBase<InlineQueryContext>(serviceProvider, pipeline), IInlineQueryDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(InlineQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return RunAsync(new InlineQueryContext(query), cancellationToken);
    }
}
