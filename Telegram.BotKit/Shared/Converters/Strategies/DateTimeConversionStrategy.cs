using System.Globalization;

namespace Telegram.BotKit.Shared.Converters.Strategies;

internal sealed class DateTimeConversionStrategy : IConversionStrategy
{
    public bool CanConvert(Type targetType) => targetType == typeof(DateTime);

    public object Convert(string input, Type targetType)
    {
        // composite parsing for date and time combinations separated by 'T' or space
        var parts = input.Split(['T', ' '], 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2 &&
            DateOnlyConversionStrategy.TryParse(parts[0], out var date) &&
            TimeOnlyConversionStrategy.TryParse(parts[1], out var time))
        {
            return date.ToDateTime(time);
        }

        if (parts.Length == 1 && DateOnlyConversionStrategy.TryParse(parts[0], out var dateOnly))
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        // lenient fallback for standard ISO-8601, timezone offsets, and textual months
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return dateTime;
        }

        throw new FormatException($"String '{input}' was not recognized as a valid DateTime.");
    }
}