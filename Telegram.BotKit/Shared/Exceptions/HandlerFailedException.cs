namespace Telegram.BotKit.Shared.Exceptions;

/// <summary>
/// Thrown when an update handler fails during execution.
/// </summary>
/// <param name="handlerType">The type of the handler that failed.</param>
/// <param name="innerException">The exception thrown by the handler.</param>
public sealed class HandlerFailedException(Type handlerType, Exception innerException)
    : BotKitException($"Handler '{handlerType.Name}' failed during execution.", innerException)
{
    /// <summary>
    /// Gets the type of the handler that failed.
    /// </summary>
    public Type HandlerType { get; } = handlerType;
}

