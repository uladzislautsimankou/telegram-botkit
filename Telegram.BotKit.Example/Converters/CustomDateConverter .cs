using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Example.Converters;

/// <summary>
/// A sample custom converter that extends DateTime parsing logic.
/// It teaches the bot to understand "today" and "tomorrow" in addition to standard formats.
/// </summary>
public class CustomDateConverter : IValueConverter
{
    public bool TryConvert(string input, Type targetType, out object? result)
    {
        // Check if this converter supports the target type.
        // We handle DateTime and Nullable<DateTime>.
        if (targetType != typeof(DateTime) && targetType != typeof(DateTime?))
        {
            result = null;
            return false; // We don't know this type, let the next converter try.
        }

        // Handle empty input (let the default logic handle nulls or validation later)
        if (string.IsNullOrWhiteSpace(input))
        {
            result = null;
            return false;
        }

        // Some Custom Logic: Handle keywords
        var normalizedInput = input.Trim().ToLowerInvariant();

        if (normalizedInput == "today")
        {
            result = DateTime.Today;
            return true; // Success! Validation stops here.
        }

        if (normalizedInput == "tomorrow")
        {
            result = DateTime.Today.AddDays(1);
            return true;
        }

        // Fallback
        // If the input is not a keyword (e.g., just "2025-01-01"), return false.
        // The framework will then call DefaultValueConverter to parse standard formats.
        result = null;
        return false;
    }
}
