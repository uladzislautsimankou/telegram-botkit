using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Example.Converters;

/// <summary>
/// Converts date strings supporting special "today" and "tomorrow" keywords.
/// </summary>
public sealed class CustomDateConverter(TimeProvider? timeProvider = null) : IValueConverter
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public bool CanConvert(Type targetType)
    {
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return actualType == typeof(DateTime) || actualType == typeof(DateOnly);
    }

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

        if (!CanConvert(context.TargetType))
        {
            return false;
        }

        var input = context.Input.AsSpan().Trim();
        if (input.IsEmpty)
        {
            return false;
        }

        var today = _timeProvider.GetLocalNow().Date;

        DateTime resolvedDate;
        if (input.Equals("today", StringComparison.OrdinalIgnoreCase))
        {
            resolvedDate = today;
        }
        else if (input.Equals("tomorrow", StringComparison.OrdinalIgnoreCase))
        {
            resolvedDate = today.AddDays(1);
        }
        else
        {
            return false;
        }

        var actualType = Nullable.GetUnderlyingType(context.TargetType) ?? context.TargetType;

        result = actualType == typeof(DateOnly)
            ? DateOnly.FromDateTime(resolvedDate)
            : resolvedDate;

        return true;
    }
}
