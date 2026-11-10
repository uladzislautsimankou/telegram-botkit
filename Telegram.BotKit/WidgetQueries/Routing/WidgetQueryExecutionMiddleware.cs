using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.WidgetQueries.Routing;

/// <summary>
/// Execution middleware responsible for running the matched widget query handler.
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve the parameter binder.</param>
internal sealed class WidgetQueryExecutionMiddleware(IServiceProvider serviceProvider) : IWidgetQueryMiddleware
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(WidgetQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context is not { MatchedRegistration: not null, MatchedWidgetQueryMetadata: not null })
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(context.MatchedRegistration.Invoker(
            context.MatchedWidgetQueryMetadata.HandlerInstance,
            context,
            serviceProvider,
            cancellationToken));
    }
}
