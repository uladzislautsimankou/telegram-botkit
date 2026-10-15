namespace Telegram.BotKit.Callbacks.Models;

/// <summary>
/// Encapsulates runtime metadata about the matched callback handler.
/// </summary>
public sealed record CallbackMetadata(Type HandlerType, object HandlerInstance);
