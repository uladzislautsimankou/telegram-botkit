namespace Telegram.BotKit.Exceptions;

/// <summary>
/// Represents the base class for all exceptions thrown within the Telegram.BotKit framework.
/// </summary>
public abstract class BotKitException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BotKitException"/> class with a specified error message.
    /// </summary>
    protected BotKitException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BotKitException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    protected BotKitException(string message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a matching handler cannot be found for a specific route key (command or callback data).
/// This typically occurs when strict routing is enabled.
/// </summary>
/// <param name="routeKey">The key (command or callback data) that failed to match any registered handler.</param>
public class RouteNotFoundException(string routeKey)
    : BotKitException($"Handler not found for route: {routeKey}")
{
    /// <summary>
    /// Gets the route key that caused the exception.
    /// </summary>
    public string RouteKey { get; } = routeKey;
}

/// <summary>
/// Thrown when a command is not found, but similar commands are available.
/// This allows the handler to suggest similar commands to the user.
/// </summary>
/// <param name="command">The command that was not found.</param>
/// <param name="suggestions">The list of similar command suggestions.</param>
public class CommandSuggestionsException(string command, List<string> suggestions)
    : BotKitException($"Unknown command '{command}'.")
{
    /// <summary>
    /// Gets the command that was not found.
    /// </summary>
    public string Command { get; } = command;

    /// <summary>
    /// Gets the list of suggested similar commands.
    /// </summary>
    public List<string> Suggestions { get; } = suggestions;
}

/// <summary>
/// Thrown when the raw input string cannot be parsed due to syntax errors.
/// For example: unclosed quotation marks in a command argument or a malformed query string in callback data.
/// </summary>
/// <param name="input">The raw input string that caused the error.</param>
/// <param name="message">The error message explaining the parsing failure.</param>
public class InputParsingException(string input, string message)
    : BotKitException(message)
{
    /// <summary>
    /// Gets the raw input that failed validation.
    /// </summary>
    public string Input { get; } = input;
}

/// <summary>
/// Represents an abstract base class for errors that occur during the binding of parameters to DTO properties.
/// </summary>
/// <param name="parameterName">The name of the parameter involved in the error.</param>
/// <param name="message">The error message.</param>
public abstract class ParameterBindingException(string parameterName, string message)
    : BotKitException(message)
{
    /// <summary>
    /// Gets the name of the parameter associated with the error.
    /// </summary>
    public string ParameterName { get; } = parameterName;
}

/// <summary>
/// Thrown when a parameter marked as required is not present in the input arguments.
/// </summary>
/// <param name="parameterName">The name of the missing parameter.</param>
public class MissingParameterException(string parameterName)
    : ParameterBindingException(parameterName, $"Required parameter '{parameterName}' is missing.");

/// <summary>
/// Thrown when a parameter value cannot be converted to the target property type.
/// For example, when trying to bind a non-numeric string to an integer property.
/// </summary>
/// <param name="parameterName">The name of the parameter.</param>
/// <param name="rawValue">The raw string value that failed conversion.</param>
/// <param name="targetType">The expected target type.</param>
public class InvalidParameterTypeException(string parameterName, string? rawValue, Type targetType)
    : ParameterBindingException(parameterName, $"Value '{rawValue}' is not valid for parameter '{parameterName}' (expected {targetType.Name}).")
{
    /// <summary>
    /// Gets the raw value that could not be converted.
    /// </summary>
    public string? RawValue { get; } = rawValue;

    /// <summary>
    /// Gets the target type that was expected.
    /// </summary>
    public Type TargetType { get; } = targetType;
}

/// <summary>
/// Thrown when a critical error occurs during the bot application startup.
/// This includes invalid tokens, network connectivity issues, or inaccessibility of the Telegram API.
/// </summary>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner exception that caused the startup failure (e.g., HttpRequestException).</param>
public class BotStartupException(string message, Exception? innerException = null)
    : BotKitException(message, innerException);