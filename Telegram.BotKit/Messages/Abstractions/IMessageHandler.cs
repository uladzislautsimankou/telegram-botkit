using Telegram.BotKit.Messages.Models;

namespace Telegram.BotKit.Messages.Abstractions;

/// <summary>
/// Defines a handler for processing incoming Telegram messages based on user states and custom matching criteria.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Gets the user state required to trigger this handler.
    /// If null, the handler is treated as global and active when the user has no state, or as a fallback if no state-specific handler matched.
    /// </summary>
    static virtual string? RequiredState => null;

    /// <summary>
    /// Determines whether the incoming message context satisfies the handler's execution criteria.
    /// </summary>
    /// <param name="context">The context containing the message and metadata.</param>
    /// <returns>True if this handler can process the message; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// public static bool CanHandle(MessageContext context) =>
    ///     context.Message is { Chat.Type: ChatType.Private, Entities: { } entities }
    ///     &amp;&amp; entities.Any(e => e.Type == MessageEntityType.CustomEmoji);
    /// </code>
    /// </example>
    static abstract bool CanHandle(MessageContext context);

    /// <summary>
    /// Gets the priority of this handler among the handlers evaluated for the same update: lower values go first, the default is 0.
    /// Handlers with equal priority keep registration order: handlers found by assembly scanning are registered
    /// ordered by full type name, handlers added with AddHandler follow in call order.
    /// Message and chosen inline result updates are processed by the first handler whose CanHandle returns true;
    /// for inline queries all matching handlers run and Order defines the order of their results.
    /// </summary>
    static virtual int Order => 0;

    /// <summary>
    /// Handles the message execution.
    /// </summary>
    /// <param name="context">The context containing the message and metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default);
}
