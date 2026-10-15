namespace Telegram.BotKit.Messages.Models;

/// <summary>
/// Encapsulates runtime metadata about the matched message handler.
/// </summary>
public sealed record MessageMetadata(Type HandlerType, object HandlerInstance);
