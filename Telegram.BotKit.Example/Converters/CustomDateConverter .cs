using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Example.Converters;

/// <summary>
/// Converts date strings supporting special "today" and "tomorrow" keywords.
/// </summary>
public class CustomDateConverter : IValueConverter
{
    public object? Convert(ValueConversionContext context)
    {
        if (TryConvert(context, out var result))
        {
            return result;
        }

        throw new FormatException(
            $"Input '{context.Input}' is not a recognized date keyword for target type '{context.TargetType.Name}'."
        );
    }

    public bool TryConvert(ValueConversionContext context, out object? result)
    {
        result = null;
        var (input, targetType, _) = context;

        // unwrap nullable types (e.g. DateTime? or DateOnly?)
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // check if this converter supports the target type
        if (actualType != typeof(DateTime) && actualType != typeof(DateTime?))
        {
            return false;
        }

        // handle empty inputs safely
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // map keyword to DateTime
        DateTime? resolvedDate = input.Trim().ToLowerInvariant() switch
        {
            "today" => DateTime.Today,
            "tomorrow" => DateTime.Today.AddDays(1),
            _ => null
        };

        if (resolvedDate is null)
        {
            return false;
        }

        result = actualType == typeof(DateOnly)
            ? DateOnly.FromDateTime(resolvedDate.Value)
            : resolvedDate.Value;

        return true;
    }
}