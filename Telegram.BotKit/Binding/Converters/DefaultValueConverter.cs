using System.ComponentModel;
using System.Globalization;
using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Binding.Converters;

internal class DefaultValueConverter : IValueConverter
{
    private static readonly string[] _dateFormats =
    [
        "yyyy-MM-dd", // ISO
        "MM/dd/yyyy", // US (стандарт Invariant)
        "dd.MM.yyyy", // RU/EU (31.12.2025)
        "dd-MM-yyyy", // RU/EU (31-12-2025)
        "yyyy/MM/dd"  // Альтернативный ISO
    ];

    public bool TryConvert(string input, Type targetType, out object? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            if (IsNullable(targetType))
            {
                return true;
            }

            return false;
        }

        // убираем nullable обертку (int? в int), чтобы TypeDescriptor сработал корректно
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // если нам нужна строка по итогу, то конвертация не нужна
        if (actualType == typeof(string))
        {
            result = input;
            return true;
        }

        if (actualType.IsEnum)
        {
            // пробуем распарсить и проверяем, что такое значение реально есть
            if (!Enum.TryParse(actualType, input, true, out var enumResult) || !Enum.IsDefined(actualType, enumResult))
            {
                throw new FormatException($"Value '{input}' is not a valid member of enum '{actualType.Name}'.");
            }

            result = enumResult;
            return true;
        }

        if (actualType == typeof(DateTime))
        {
            // Пытаемся распарсить по списку форматов
            if (!DateTime.TryParseExact(input, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                throw new FormatException($"String '{input}' was not recognized as a valid DateTime. Supported formats: {string.Join(", ", _dateFormats)}");
            }

            result = date;
            return true;
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

                result = converter.ConvertFromInvariantString(input);
                return true;
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to convert value '{input}' to type '{actualType.Name}'.", ex);
            }
        }

        return false;
    }

    private static bool IsFloatingPoint(Type type) =>
        type == typeof(double) || type == typeof(float) || type == typeof(decimal);

    private static bool IsNullable(Type type)
    {
        // !IsValueType = cсылочный тип (string, class) -> да
        // Nullable.GetUnderlyingType != null = Nullable struct (int?) -> да
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}