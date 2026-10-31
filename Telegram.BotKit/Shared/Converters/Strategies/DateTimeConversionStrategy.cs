using System.Globalization;

namespace Telegram.BotKit.Shared.Converters.Strategies;

/// <summary>
/// Converts a date, a date with a time, or an ISO 8601 / textual date to <see cref="DateTime"/>.
/// </summary>
internal sealed class DateTimeConversionStrategy : IConversionStrategy
{
    /// <inheritdoc />
    public bool CanConvert(Type targetType) => targetType == typeof(DateTime);

    /// <inheritdoc />
    public bool TryConvert(string input, Type targetType, out object? result)
    {
        result = null;

        var span = input.AsSpan().Trim();
        var separatorIndex = span.IndexOfAny('T', ' ');

        if (separatorIndex > 0)
        {
            if (DateOnlyConversionStrategy.TryParse(span[..separatorIndex], out var date) &&
                TimeOnlyConversionStrategy.TryParse(span[(separatorIndex + 1)..], out var time))
            {
                result = date.ToDateTime(time);
                return true;
            }
        }
        else if (DateOnlyConversionStrategy.TryParse(span, out var dateOnly))
        {
            result = dateOnly.ToDateTime(TimeOnly.MinValue);
            return true;
        }

        // fallback for standard iso-8601 strings and textual dates
        if (DateTime.TryParse(span, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            result = dateTime;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public string DescribeFailure(string input, Type targetType) =>
        $"String '{input}' was not recognized as a valid DateTime.";
}
