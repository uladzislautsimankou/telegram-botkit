using System.Globalization;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Callbacks.Building;

/// <summary>
/// Formats parameter values in the forms the value converters accept: by a registered converter when one formats the type,
/// otherwise in the forms the default converter accepts.
/// </summary>
internal static class CallbackValueFormatter
{
    /// <summary>
    /// Formats a parameter value as text. A DateTime keeps neither its Kind nor fractional seconds.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="converters">The converters to ask first, the highest priority (registered last) first; null for none.</param>
    /// <returns>The text form of the value.</returns>
    public static string Format(object value, IReadOnlyList<IValueConverter>? converters = null)
    {
        if (converters is not null)
        {
            var type = value.GetType();

            for (var i = 0; i < converters.Count; i++)
            {
                if (converters[i].CanConvert(type) && converters[i].TryFormat(value, out var text))
                {
                    return text;
                }
            }
        }

        return FormatBuiltIn(value);
    }

    private static string FormatBuiltIn(object value) => value switch
    {
        string s => s,
        bool b => b ? "true" : "false",
        Enum e => Enum.Format(e.GetType(), e, "D"),
        Guid g => g.ToString("N"),
        DateTime dt => dt.TimeOfDay == TimeSpan.Zero
            ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : dt.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly t => t.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
