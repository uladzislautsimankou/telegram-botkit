namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Represents a specialized middleware responsible for global error handling during command execution.
/// This middleware is registered as the first step in the pipeline to catch exceptions from subsequent components.
/// </summary>
public interface ICommandErrorHandlerMiddleware : ICommandMiddleware
{
}