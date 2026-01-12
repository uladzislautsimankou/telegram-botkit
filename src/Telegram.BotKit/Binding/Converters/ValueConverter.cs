using System.ComponentModel;
using System.Globalization;

namespace Telegram.BotKit.Binding.Converters;

internal static class ValueConverter
{
    private static readonly string[] _dateFormats =
    [
        "yyyy-MM-dd", // ISO
        "MM/dd/yyyy", // US (стандарт Invariant)
        "dd.MM.yyyy", // RU/EU (31.12.2025)
        "dd-MM-yyyy", // RU/EU (31-12-2025)
        "yyyy/MM/dd"  // Альтернативный ISO
    ];

    public static object? Convert(string? input, Type targetType)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        // убираем nullable обертку (int? в int), чтобы TypeDescriptor сработал корректно
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // если нам нужна строка по итогу, то конвертация не нужна
        if (actualType == typeof(string)) return input;

        if (actualType.IsEnum)
        {
            // пробуем распарсить и проверяем, что такое значение реально есть
            if (!Enum.TryParse(actualType, input, true, out var result) || !Enum.IsDefined(actualType, result))
            {
                throw new FormatException($"Value '{input}' is not a valid member of enum '{actualType.Name}'.");
            }

            return result;
        }

        if (actualType == typeof(DateTime))
        {
            // Пытаемся распарсить по списку форматов
            if (!DateTime.TryParseExact(input, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                throw new FormatException($"String '{input}' was not recognized as a valid DateTime. Supported formats: {string.Join(", ", _dateFormats)}");
            }

            return date;
        }

        var converter = TypeDescriptor.GetConverter(actualType);

        if (converter != null && converter.CanConvertFrom(typeof(string)))
        {
            try
            {
                if (IsFloatingPoint(actualType))
                {
                    input = input.Replace(',', '.');
                }

                return converter.ConvertFromInvariantString(input);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to convert value '{input}' to type '{actualType.Name}'.", ex);
            }
        }

        throw new NotSupportedException($"No type converter found for type '{actualType.Name}'.");
    }

    private static bool IsFloatingPoint(Type type) =>
        type == typeof(double) || type == typeof(float) || type == typeof(decimal);
}