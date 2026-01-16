namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Represents a delegate that invokes the next component in the middleware pipeline.
/// </summary>
/// <returns>A task that represents the completion of the next component's execution.</returns>
public delegate Task NextDelegate();