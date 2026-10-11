using System.Globalization;

namespace Telegram.BotKit.Shared.Converters.Strategies;

internal sealed class DateOnlyConversionStrategy : IConversionStrategy
{
    private static readonly string[] _formats =
    [
        "yyyy-MM-dd", // ISO
        "dd.MM.yyyy", // EU / RU
        "dd-MM-yyyy",
        "yyyy/MM/dd",
        "MM/dd/yyyy"  // US
    ];

    public bool CanConvert(Type targetType) => targetType == typeof(DateOnly);

    public object Convert(string input, Type targetType)
    {
        if (TryParse(input, out var date))
        {
            return date;
        }

        throw new FormatException($"String '{input}' was not recognized as a valid DateOnly. Supported formats: {string.Join(", ", _formats)}");
    }

    internal static bool TryParse(string input, out DateOnly date) =>
        DateOnly.TryParseExact(input, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}