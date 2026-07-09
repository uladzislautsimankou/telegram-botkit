using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit;

/// <summary>
/// Represents the context of an incoming standard message update within the processing pipeline.
/// </summary>
public record MessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContext"/> class.
    /// </summary>
    /// <param name="message">The message received from Telegram.</param>
    /// <exception cref="ArgumentNullException">Thrown if the message is null.</exception>
    public MessageContext(Message message) => Message = message ?? throw new ArgumentNullException(nameof(message));

    /// <summary>
    /// Gets the original message received from Telegram.
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// Gets or sets the active state of the user who sent the message.
    /// Used by the routing middleware to match state-specific message handlers.
    /// </summary>
    public string? UserState { get; set; }

    /// <summary>
    /// Gets or sets the matched handler assigned to process this message.
    /// </summary>
    internal IMessageHandler? MatchedHandler { get; set; }
}