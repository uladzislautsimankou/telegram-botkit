using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Callbacks.Routing;

/// <summary>
/// Routing middleware responsible for identifying the correct handler based on the callback context key.
/// </summary>
internal sealed class CallbackRoutingMiddleware(
    CallbackHandlerRegistry registry,
    IServiceProvider serviceProvider
    ) : ICallbackMiddleware
{
    public async Task InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (!registry.TryGetInvokerType(context.Key, out var invokerType))
        {
            throw new RouteNotFoundException(context.Key);
        }

        context.MatchedInvoker = (ICallbackHandlerInvoker)serviceProvider.GetRequiredService(invokerType!);
        await next();
    }
}