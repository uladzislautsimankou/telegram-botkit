using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Callbacks.Routing;

/// <summary>
/// Routing middleware responsible for identifying the correct handler based on the callback context key.
/// </summary>
internal sealed class CallbackRoutingMiddleware(
    CallbackHandlerRegistry registry,
    IServiceProvider serviceProvider) : ICallbackMiddleware
{
    public ValueTask InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (!registry.TryGetRegistration(context.Key, out var registration))
        {
            throw new RouteNotFoundException(context.Key);
        }

        var handlerInstance = registration!.Factory(serviceProvider);
        context.MatchedRegistration = registration;
        context.MatchedCallbackMetadata = new CallbackMetadata(registration.HandlerType, handlerInstance);

        return next();
    }
}
