using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.ChosenInlineResults.Routing;

/// <summary>
/// Routing middleware responsible for identifying the appropriate chosen inline result handler.
/// </summary>
internal sealed class ChosenInlineResultRoutingMiddleware(
    ChosenInlineResultHandlerRegistry registry,
    IServiceProvider serviceProvider) : IChosenInlineResultMiddleware
{
    public ValueTask InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        var handlers = registry.GetHandlers();

        for (var i = 0; i < handlers.Length; i++)
        {
            ref readonly var descriptor = ref handlers[i];

            if (descriptor.CanHandle(context.Result))
            {
                var instance = (IChosenInlineResultHandler)descriptor.Factory(serviceProvider);
                context.MatchedHandler = instance;
                context.MatchedChosenInlineResultMetadata = new ChosenInlineResultMetadata(descriptor.HandlerType, instance);
                break;
            }
        }

        return next();
    }
}
