namespace Telegram.BotKit.Shared.Exceptions;

/// <summary>
/// Thrown when a parameter marked as required is not present in the input arguments.
/// </summary>
/// <param name="parameterName">The name of the missing parameter.</param>
public sealed class MissingParameterException(string parameterName)
    : ParameterBindingException(parameterName, $"Required parameter '{parameterName}' is missing.");

