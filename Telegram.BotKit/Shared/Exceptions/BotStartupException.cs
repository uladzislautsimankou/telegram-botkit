namespace Telegram.BotKit.Shared.Exceptions;

/// <summary>
/// Thrown when a critical error occurs during bot initialization or startup.
/// </summary>
/// <param name="message">The message that describes the error.</param>
/// <param name="innerException">The exception that is the cause of this exception, or null.</param>
public sealed class BotStartupException(string message, Exception? innerException = null)
    : BotKitException(message, innerException);

