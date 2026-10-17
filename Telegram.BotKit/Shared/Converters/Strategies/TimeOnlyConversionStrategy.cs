using System.Globalization;

namespace Telegram.BotKit.Shared.Converters.Strategies;

/// <summary>
/// Converts text to <see cref="TimeOnly"/>.
/// </summary>
internal sealed class TimeOnlyConversionStrategy : IConversionStrategy
{
    /// <inheritdoc />
    public bool CanConvert(Type targetType) => targetType == typeof(TimeOnly);

    /// <inheritdoc />
    public bool TryConvert(string input, Type targetType, out object? result)
    {
        if (TryParse(input.AsSpan(), out var time))
        {
            result = time;
            return true;
        }

        result = null;
        return false;
    }

    /// <inheritdoc />
    public string DescribeFailure(string input, Type targetType) =>
        $"String '{input}' was not recognized as a valid TimeOnly.";

    /// <summary>
    /// Parses a time of day.
    /// </summary>
    /// <param name="input">The text to parse.</param>
    /// <param name="time">The parsed time when the parsing succeeded.</param>
    /// <returns>True if the input was parsed; otherwise, false.</returns>
    internal static bool TryParse(ReadOnlySpan<char> input, out TimeOnly time) =>
        TimeOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
}
