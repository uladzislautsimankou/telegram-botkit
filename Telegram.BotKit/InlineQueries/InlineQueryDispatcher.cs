using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.InlineQueries;

/// <summary>
/// Dispatches incoming inline query updates by executing them through the compiled middleware pipeline.
/// </summary>
internal sealed class InlineQueryDispatcher(
    IServiceProvider serviceProvider,
    PipelineDelegate<InlineQueryContext> pipeline) : IInlineQueryDispatcher
{
    public async Task DispatchAsync(InlineQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var context = new InlineQueryContext(query);

        // dedicated scope for the query lifecycle
        await using var scope = serviceProvider.CreateAsyncScope();

        // execute the precompiled middleware delegate chain
        await pipeline(context, scope.ServiceProvider, cancellationToken);
    }
}