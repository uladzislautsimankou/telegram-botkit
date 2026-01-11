using System.ComponentModel;

namespace Telegram.BotKit.Binding.Converters;

internal static class ValueConverter
{
    public static object? Convert(string? input, Type targetType)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        // убираем nullable обертку (int? в int), чтобы TypeDescriptor сработал корректно
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // если нам нужна строка по итогу, то конвертация не нужна
        if (actualType == typeof(string)) return input;

        if (actualType.IsEnum)
        {
            try
            {
                return Enum.Parse(actualType, input, ignoreCase: true);
            }
            catch
            {
                throw new FormatException($"Value '{input}' is not a valid member of enum '{actualType.Name}'.");
            }
        }

        var converter = TypeDescriptor.GetConverter(actualType);

        if (converter != null && converter.CanConvertFrom(typeof(string)))
        {
            try
            {
                return converter.ConvertFromInvariantString(input);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to convert value '{input}' to type '{actualType.Name}'.", ex);
            }
        }

        throw new NotSupportedException($"No type converter found for type '{actualType.Name}'.");
    }
}