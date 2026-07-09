using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit;

public record MessageContext
{
    public MessageContext(Message message) => Message = message ?? throw new ArgumentNullException(nameof(message));

    public Message Message { get; }

    public string? UserState { get; set; }

    internal IMessageHandler? MatchedHandler { get; set; }
}