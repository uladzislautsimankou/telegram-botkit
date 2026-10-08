using Telegram.BotKit.Messages.Models;

namespace Telegram.BotKit.Messages.Abstractions;

/// <summary>
/// Defines a handler for processing incoming Telegram messages based on user states and custom matching criteria.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Gets the user state required to trigger this handler.
    /// If this returns <see langword="null"/>, the handler is treated as global and is active when the user has no state.
    /// </summary>
    static virtual string? RequiredState => null;

    /// <summary>
    /// Determines whether the incoming message context satisfies the handler's execution criteria.
    /// </summary>
    /// <param name="context">The context containing the message and metadata.</param>
    /// <returns><see langword="true"/> if this handler can process the message; otherwise, <see langword="false"/>.</returns>
    /// <example>
    /// <code>
    /// public bool CanHandle(MessageContext context) =>
    ///     context.Message is { Chat.Type: ChatType.Private, Entities: { } entities }
    ///     && entities.Any(e => e.Type == MessageEntityType.CustomEmoji);
    /// </code>
    /// </example>
    bool CanHandle(MessageContext context);

    /// <summary>
    /// Handles the message execution.
    /// </summary>
    /// <param name="context">The context containing the message and metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(MessageContext context, CancellationToken cancellationToken);
}