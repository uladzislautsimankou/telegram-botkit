namespace Telegram.BotKit.Shared.Exceptions;

/// <summary>
/// Represents an abstract base class for errors that occur during parameter binding.
/// </summary>
/// <param name="parameterName">The name of the parameter that could not be bound.</param>
/// <param name="message">The message that describes the error.</param>
public abstract class ParameterBindingException(string parameterName, string message)
    : BotKitException(message)
{
    /// <summary>
    /// Gets the name of the parameter that could not be bound.
    /// </summary>
    public string ParameterName { get; } = parameterName;
}

