using System.Globalization;

namespace Telegram.BotKit.Shared.Converters.Strategies;

internal sealed class DateTimeConversionStrategy : IConversionStrategy
{
    public bool CanConvert(Type targetType) => targetType == typeof(DateTime);

    public object Convert(string input, Type targetType)
    {
        var span = input.AsSpan().Trim();
        var separatorIndex = span.IndexOfAny('T', ' ');

        if (separatorIndex > 0)
        {
            var dateSpan = span[..separatorIndex];
            var timeSpan = span[(separatorIndex + 1)..];

            if (DateOnlyConversionStrategy.TryParse(dateSpan, out var date) &&
                TimeOnlyConversionStrategy.TryParse(timeSpan, out var time))
            {
                return date.ToDateTime(time);
            }
        }
        else if (DateOnlyConversionStrategy.TryParse(span, out var dateOnly))
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        // fallback for standard iso-8601 strings and textual dates
        if (DateTime.TryParse(span, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return dateTime;
        }

        throw new FormatException($"String '{input}' was not recognized as a valid DateTime.");
    }
}
