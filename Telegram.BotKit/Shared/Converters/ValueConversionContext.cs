namespace Telegram.BotKit.Shared.Converters;

/// <summary>
/// Provides contextual information for value conversion.
/// </summary>
/// <param name="Input">The raw string input to convert.</param>
/// <param name="TargetType">The target property or argument type.</param>
/// <param name="TargetObject">The instance of the model or DTO being populated.</param>
public sealed record ValueConversionContext(
    string Input,
    Type TargetType,
    object? TargetObject = null
);