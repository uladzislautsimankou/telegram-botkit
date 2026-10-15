using System.Globalization;

namespace Telegram.BotKit.Shared.Converters.Strategies;

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

    public bool CanConvert(Type targetType) => targetType == typeof(DateOnly);

    public object Convert(string input, Type targetType)
    {
        if (TryParse(input.AsSpan(), out var date))
        {
            return date;
        }

        throw new FormatException($"String '{input}' was not recognized as a valid DateOnly. Supported formats: {string.Join(", ", Formats)}");
    }

    internal static bool TryParse(ReadOnlySpan<char> input, out DateOnly date) =>
        DateOnly.TryParseExact(input, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
