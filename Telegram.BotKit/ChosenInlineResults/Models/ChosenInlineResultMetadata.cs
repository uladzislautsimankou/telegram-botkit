namespace Telegram.BotKit.ChosenInlineResults.Models;

/// <summary>
/// Encapsulates runtime metadata about the matched chosen inline result handler.
/// </summary>
public sealed record ChosenInlineResultMetadata(Type HandlerType, object HandlerInstance);