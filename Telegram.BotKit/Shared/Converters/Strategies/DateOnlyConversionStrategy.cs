using System.Globalization;

namespace Telegram.BotKit.Shared.Converters.Strategies;

/// <summary>
/// Converts text in one of the supported formats (e.g. yyyy-MM-dd, dd.MM.yyyy) to <see cref="DateOnly"/>.
/// </summary>
internal sealed class DateOnlyConversionStrategy : IConversionStrategy
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd",
        "dd.MM.yyyy",
        "dd-MM-yyyy",
        "yyyy/MM/dd",
        "MM/dd/yyyy"
    ];

    /// <inheritdoc />
    public bool CanConvert(Type targetType) => targetType == typeof(DateOnly);

    /// <inheritdoc />
    public bool TryConvert(string input, Type targetType, out object? result)
    {
        if (TryParse(input.AsSpan(), out var date))
        {
            result = date;
            return true;
        }

        result = null;
        return false;
    }

    /// <inheritdoc />
    public string DescribeFailure(string input, Type targetType) =>
        $"String '{input}' was not recognized as a valid DateOnly. Supported formats: {string.Join(", ", Formats)}";

    /// <summary>
    /// Parses a date in one of the supported formats.
    /// </summary>
    /// <param name="input">The text to parse.</param>
    /// <param name="date">The parsed date when the parsing succeeded.</param>
    /// <returns>True if the input was parsed; otherwise, false.</returns>
    internal static bool TryParse(ReadOnlySpan<char> input, out DateOnly date) =>
        DateOnly.TryParseExact(input, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
