namespace Telegram.BotKit.Shared.Exceptions;

/// <summary>
/// Thrown when a parameter value cannot be converted to the target property type.
/// </summary>
/// <param name="parameterName">The name of the parameter.</param>
/// <param name="rawValue">The raw value that could not be converted.</param>
/// <param name="targetType">The type the value was converted to.</param>
public sealed class InvalidParameterTypeException(string parameterName, string? rawValue, Type targetType)
    : ParameterBindingException(parameterName, $"Value '{rawValue}' is not valid for parameter '{parameterName}' (expected {targetType.Name}).")
{
    /// <summary>
    /// Gets the raw value that could not be converted.
    /// </summary>
    public string? RawValue { get; } = rawValue;
    /// <summary>
    /// Gets the type the value was converted to.
    /// </summary>
    public Type TargetType { get; } = targetType;
}

