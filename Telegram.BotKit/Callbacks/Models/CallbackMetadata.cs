namespace Telegram.BotKit.Callbacks.Models;

/// <summary>
/// Encapsulates runtime metadata about the matched callback handler.
/// </summary>
public record CallbackMetadata(Type HandlerType, object HandlerInstance);