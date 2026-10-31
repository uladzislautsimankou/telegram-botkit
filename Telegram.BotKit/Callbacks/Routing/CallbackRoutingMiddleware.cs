using Microsoft.Extensions.Logging;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Callbacks.Routing;

/// <summary>
/// Routing middleware responsible for identifying the callback handler registered for the route key of the callback data.
/// </summary>
/// <param name="registry">The registry of callback handlers.</param>
/// <param name="serviceProvider">The service provider used to create the matched handler.</param>
/// <param name="logger">The logger used to report callback data without a registered handler.</param>
internal sealed class CallbackRoutingMiddleware(
    CallbackHandlerRegistry registry,
    IServiceProvider serviceProvider,
    ILogger<CallbackRoutingMiddleware> logger) : ICallbackMiddleware
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (registry.TryGetRegistration(context.Key, out var registration))
        {
            context.MatchedRegistration = registration;

            // the handler is created lazily by the execution middleware
            context.MatchedCallbackMetadata = new CallbackMetadata(
                registration.HandlerType,
                () => registration.Factory(serviceProvider));
        }
        else
        {
            // typically a button of a message sent before a deployment that renamed or removed the handler:
            // like the other routers, leave the context unmatched and continue, so that later middleware can react
            // (the execution middleware skips unmatched contexts)
            logger.LogWarning("No callback handler is registered for key '{CallbackKey}'", context.Key);
        }

        return next();
    }
}

