using Microsoft.Extensions.Logging;
using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.WidgetQueries.Routing;

/// <summary>
/// Routing middleware responsible for identifying the widget query handler registered for the route key of the query data.
/// </summary>
/// <param name="registry">The registry of widget query handlers.</param>
/// <param name="serviceProvider">The service provider used to create the matched handler.</param>
/// <param name="logger">The logger used to report query data without a registered handler.</param>
internal sealed class WidgetQueryRoutingMiddleware(
    WidgetQueryHandlerRegistry registry,
    IServiceProvider serviceProvider,
    ILogger<WidgetQueryRoutingMiddleware> logger) : IWidgetQueryMiddleware
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(WidgetQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (registry.TryGetRegistration(context.Key, out var registration))
        {
            context.MatchedRegistration = registration;

            // the handler is created lazily by the execution middleware
            context.MatchedWidgetQueryMetadata = new WidgetQueryMetadata(
                registration.HandlerType,
                () => registration.Factory(serviceProvider));
        }
        else
        {
            // typically a widget sent before a deployment that renamed or removed the handler:
            // leave the context unmatched and continue, so that later middleware can react
            // (the execution middleware skips unmatched contexts)
            logger.LogWarning("No widget query handler is registered for key '{WidgetQueryKey}'", context.Key);
        }

        return next();
    }
}
