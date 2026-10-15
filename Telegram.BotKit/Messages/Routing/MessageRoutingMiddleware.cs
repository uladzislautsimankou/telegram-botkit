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
    IServiceProvider serviceProvider) : IMessageMiddleware
{
    public ValueTask InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        IMessageHandler? matched = null;

        // find a handler bound specifically to the current user state
        if (!string.IsNullOrWhiteSpace(context.UserState))
        {
            matched = FindMatch(registry.GetHandlersForState(context.UserState.Trim()), context);
        }

        // fallback to global state-agnostic handlers when no state-specific handler matched
        matched ??= FindMatch(registry.GetGlobalHandlers(), context);

        if (matched is null)
        {
            return ValueTask.CompletedTask;
        }

        context.MatchedHandler = matched;
        context.MatchedMessageMetadata = new MessageMetadata(matched.GetType(), matched);

        return next();
    }

    private IMessageHandler? FindMatch(ReadOnlySpan<MessageHandlerDescriptor> descriptors, MessageContext context)
    {
        for (var i = 0; i < descriptors.Length; i++)
        {
            ref readonly var descriptor = ref descriptors[i];

            if (descriptor.CanHandle(context))
            {
                return (IMessageHandler)descriptor.Factory(serviceProvider);
            }
        }

        return null;
    }
}
