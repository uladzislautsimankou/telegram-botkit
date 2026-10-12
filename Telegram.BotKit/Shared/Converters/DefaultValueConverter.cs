using Telegram.BotKit.Shared.Converters.Strategies;

namespace Telegram.BotKit.Shared.Converters;

internal sealed class DefaultValueConverter : IValueConverter
{
    private static readonly IConversionStrategy[] Strategies =
    [
        new EnumConversionStrategy(),
        new DateTimeConversionStrategy(),
        new DateOnlyConversionStrategy(),
        new TimeOnlyConversionStrategy(),
        new TypeDescriptorConversionStrategy()
    ];

    public bool CanConvert(Type targetType)
    {
        if (targetType == typeof(string))
        {
            return true;
        }

        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        for (var i = 0; i < Strategies.Length; i++)
        {
            if (Strategies[i].CanConvert(actualType))
            {
                return true;
            }
        }

        return false;
    }

    public object? Convert(ValueConversionContext context)
    {
        var input = context.Input;
        var targetType = context.TargetType;

        if (targetType == typeof(string))
        {
            return input;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            if (IsNullable(targetType))
            {
                return null;
            }

            throw new FormatException($"Cannot convert empty string to non-nullable type '{targetType.Name}'.");
        }

        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        for (var i = 0; i < Strategies.Length; i++)
        {
            var strategy = Strategies[i];
            if (strategy.CanConvert(actualType))
            {
                return strategy.Convert(input, actualType);
            }
        }

        throw new NotSupportedException($"No conversion strategy found for target type '{targetType.Name}'.");
    }

    public bool TryConvert(ValueConversionContext context, out object? result)
    {
        result = null;

        try
        {
            result = Convert(context);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException)
        {
            return false;
        }
    }

    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}