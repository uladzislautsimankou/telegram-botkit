namespace Telegram.BotKit.Shared.Exceptions;

/// <summary>
/// Represents the base class for all exceptions thrown within the Telegram.BotKit framework.
/// </summary>
public abstract class BotKitException : Exception
{
    protected BotKitException(string message) : base(message) { }
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

/// <summary>
/// Thrown when the raw input string cannot be parsed due to syntax errors.
/// </summary>
public sealed class InputParsingException(string input, string message)
    : BotKitException(message)
{
    public string Input { get; } = input;
}

/// <summary>
/// Represents an abstract base class for errors that occur during parameter binding.
/// </summary>
public abstract class ParameterBindingException(string parameterName, string message)
    : BotKitException(message)
{
    public string ParameterName { get; } = parameterName;
}

/// <summary>
/// Thrown when a parameter marked as required is not present in the input arguments.
/// </summary>
public sealed class MissingParameterException(string parameterName)
    : ParameterBindingException(parameterName, $"Required parameter '{parameterName}' is missing.");

/// <summary>
/// Thrown when a parameter value cannot be converted to the target property type.
/// </summary>
public sealed class InvalidParameterTypeException(string parameterName, string? rawValue, Type targetType)
    : ParameterBindingException(parameterName, $"Value '{rawValue}' is not valid for parameter '{parameterName}' (expected {targetType.Name}).")
{
    public string? RawValue { get; } = rawValue;
    public Type TargetType { get; } = targetType;
}

/// <summary>
/// Thrown when a critical error occurs during bot initialization or startup.
/// </summary>
public sealed class BotStartupException(string message, Exception? innerException = null)
    : BotKitException(message, innerException);

/// <summary>
/// Thrown when an update handler fails during execution.
/// </summary>
public sealed class HandlerFailedException(Type handlerType, Exception innerException)
    : BotKitException($"Handler '{handlerType.Name}' failed during execution.", innerException)
{
    public Type HandlerType { get; } = handlerType;
}
