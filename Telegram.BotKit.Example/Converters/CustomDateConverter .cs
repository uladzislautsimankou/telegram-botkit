using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Example.Converters;

/// <summary>
/// Converts date strings supporting special "today" and "tomorrow" keywords.
/// </summary>
public class CustomDateConverter : IValueConverter
{
    public bool TryConvert(string input, Type targetType, out object? result)
    {
        // check if this converter supports the target type
        if (targetType != typeof(DateTime) && targetType != typeof(DateTime?))
        {
            result = null;
            return false;
        }

        // handle empty inputs safely
        if (string.IsNullOrWhiteSpace(input))
        {
            result = null;
            return false;
        }

        // evaluate special date keywords
        var normalizedInput = input.Trim().ToLowerInvariant();

        if (normalizedInput == "today")
        {
            result = DateTime.Today;
            return true;
        }

        if (normalizedInput == "tomorrow")
        {
            result = DateTime.Today.AddDays(1);
            return true;
        }

        // defer to next standard converter
        result = null;
        return false;
    }
}