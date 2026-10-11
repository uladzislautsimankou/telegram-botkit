using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// Routing middleware responsible for resolving the appropriate message handler based on user states and custom criteria.
/// </summary>
internal sealed class MessageRoutingMiddleware(
    MessageHandlerRegistry registry,
    IServiceProvider serviceProvider
    ) : IMessageMiddleware
{
    public async Task InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        IMessageHandler? matched = null;

        // find a handler bound specifically to the current user state
        if (!string.IsNullOrWhiteSpace(context.UserState))
        {
            var stateHandlerTypes = registry.GetHandlerTypesForState(context.UserState.Trim());
            for (var i = 0; i < stateHandlerTypes.Count; i++)
            {
                var handler = (IMessageHandler)serviceProvider.GetRequiredService(stateHandlerTypes[i]);
                if (handler.CanHandle(context))
                {
                    matched = handler;
                    break;
                }
            }
        }

        // if no state-specific handler matches or no state is active, look for a global handler
        if (matched is null)
        {
            var globalHandlerTypes = registry.GetGlobalHandlerTypes();
            for (var i = 0; i < globalHandlerTypes.Count; i++)
            {
                var handler = (IMessageHandler)serviceProvider.GetRequiredService(globalHandlerTypes[i]);
                if (handler.CanHandle(context))
                {
                    matched = handler;
                    break;
                }
            }
        }

        if (matched is not null)
        {
            context.MatchedHandler = matched;
            context.MatchedMessageMetadata = new MessageMetadata(matched.GetType(), matched);
            await next();
        }
    }
}