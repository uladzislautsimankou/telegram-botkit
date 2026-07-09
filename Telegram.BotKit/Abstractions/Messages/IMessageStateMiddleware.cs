namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Represents a middleware responsible for loading and resolving the active conversational state
/// (e.g., scoped to a specific user, chat, or custom context).
/// This runs early in the pipeline to ensure routing components can match handlers based on the resolved state.
/// </summary>
public interface IMessageStateMiddleware : IMessageMiddleware
{
}