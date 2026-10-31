using Telegram.Bot.Types;

namespace Telegram.BotKit.Messages.Models;

/// <summary>
/// Represents the context of an incoming standard message update within the processing pipeline.
/// </summary>
public sealed class MessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContext"/> class.
    /// Ensures that the incoming message instance is not null before processing.
    /// </summary>
    /// <param name="message">The message received from the Telegram Bot API.</param>
    /// <exception cref="ArgumentNullException">Thrown when the incoming message parameter is null.</exception>
    public MessageContext(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Message = message;
    }

    /// <summary>
    /// Gets the original, unparsed message containing chat details, sender info, text, media, or other update entities.
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// Gets or sets the active dialog state of the user who sent the message.
    /// Used by the routing middleware to locate and trigger state-specific conversation handlers.
    /// </summary>
    public string? UserState { get; set; }

    /// <summary>
    /// Gets or sets the matched message metadata containing details about the handler instance and its resolved type.
    /// </summary>
    public MessageMetadata? MatchedMessageMetadata { get; set; }
}
