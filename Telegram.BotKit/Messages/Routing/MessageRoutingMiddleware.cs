using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// Routing middleware responsible for resolving the appropriate message handler based on user states and custom criteria.
/// </summary>
/// <param name="registry">The registry of message handlers.</param>
/// <param name="serviceProvider">The service provider used to create the matched handler.</param>
internal sealed class MessageRoutingMiddleware(
    MessageHandlerRegistry registry,
    IServiceProvider serviceProvider) : IMessageMiddleware
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        MessageHandlerDescriptor? matched = null;

        // find a handler bound specifically to the current user state
        if (!string.IsNullOrWhiteSpace(context.UserState))
        {
            matched = FindMatch(registry.GetHandlersForState(context.UserState.Trim()), context);
        }

        // fallback to global state-agnostic handlers when no state-specific handler matched
        matched ??= FindMatch(registry.GetGlobalHandlers(), context);

        if (matched is { } descriptor)
        {
            // the handler is created lazily by the execution middleware
            context.MatchedMessageMetadata = new MessageMetadata(
                descriptor.HandlerType,
                () => descriptor.Factory(serviceProvider));
        }

        return next();
    }

    private static MessageHandlerDescriptor? FindMatch(
        ReadOnlySpan<MessageHandlerDescriptor> descriptors,
        MessageContext context)
    {
        for (var i = 0; i < descriptors.Length; i++)
        {
            if (descriptors[i].CanHandle(context))
            {
                return descriptors[i];
            }
        }

        return null;
    }
}
