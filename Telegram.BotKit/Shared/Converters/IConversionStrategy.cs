namespace Telegram.BotKit.Shared.Converters;

/// <summary>
/// Defines an internal strategy for converting string input to a specific target type.
/// </summary>
internal interface IConversionStrategy
{
    /// <summary>
    /// Determines whether this strategy can convert to the specified target type.
    /// </summary>
    /// <param name="targetType">The type to convert to, without the nullable wrapper.</param>
    /// <returns>True if the strategy supports the type; otherwise, false.</returns>
    bool CanConvert(Type targetType);

    /// <summary>
    /// Attempts to convert the input. Malformed input is reported by returning false, never by throwing.
    /// </summary>
    /// <param name="input">The raw text to convert.</param>
    /// <param name="targetType">The type to convert to, without the nullable wrapper.</param>
    /// <param name="result">The converted value when the conversion succeeded; otherwise, null.</param>
    /// <returns>True if the input was converted; otherwise, false.</returns>
    bool TryConvert(string input, Type targetType, out object? result);

    /// <summary>
    /// Describes why the input could not be converted. Used to build the message of the thrown exception.
    /// </summary>
    /// <param name="input">The raw text that could not be converted.</param>
    /// <param name="targetType">The type that was requested.</param>
    /// <returns>A human-readable description of the failure.</returns>
    string DescribeFailure(string input, Type targetType) =>
        $"Failed to convert value '{input}' to type '{targetType.Name}'.";
}
