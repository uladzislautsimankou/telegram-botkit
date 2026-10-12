using System.Globalization;

namespace Telegram.BotKit.Shared.Converters.Strategies;

internal sealed class TimeOnlyConversionStrategy : IConversionStrategy
{
    public bool CanConvert(Type targetType) => targetType == typeof(TimeOnly);

    public object Convert(string input, Type targetType)
    {
        if (TryParse(input.AsSpan(), out var time))
        {
            return time;
        }

        throw new FormatException($"String '{input}' was not recognized as a valid TimeOnly.");
    }

    internal static bool TryParse(ReadOnlySpan<char> input, out TimeOnly time) =>
        TimeOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
}