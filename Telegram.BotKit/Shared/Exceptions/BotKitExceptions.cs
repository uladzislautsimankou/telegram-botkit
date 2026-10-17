namespace Telegram.BotKit.Shared.Exceptions;

/// <summary>
/// Represents the base class for all exceptions thrown within the Telegram.BotKit framework.
/// </summary>
public abstract class BotKitException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BotKitException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected BotKitException(string message) : base(message) { }
    /// <summary>
    /// Initializes a new instance of the <see cref="BotKitException"/> class with a specified error message and the exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception, or null.</param>
    protected BotKitException(string message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a matching handler cannot be found for a specific route key.
/// </summary>
public sealed class RouteNotFoundException(string routeKey)
    : BotKitException($"Handler not found for route: {routeKey}")
{
    public string RouteKey { get; } = routeKey;
}

