using System.ComponentModel;
using System.Globalization;
using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Binding.Converters;

internal class DefaultValueConverter : IValueConverter
{
    private static readonly string[] _dateFormats =
    [
        "yyyy-MM-dd", // ISO
        "MM/dd/yyyy", // US (standard Invariant)
        "dd.MM.yyyy", // RU/EU (31.12.2025)
        "dd-MM-yyyy", // RU/EU (31-12-2025)
        "yyyy/MM/dd"  // alternative ISO
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

        // remove the nullable wrapper (e.g., int? to int) to allow TypeDescriptor to work correctly
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // if the target type is already a string, no conversion is required
        if (actualType == typeof(string))
        {
            result = input;
            return true;
        }

        if (actualType.IsEnum)
        {
            // attempt to parse the enum and verify that the member is explicitly defined
            if (!Enum.TryParse(actualType, input, true, out var enumResult) || !Enum.IsDefined(actualType, enumResult))
            {
                throw new FormatException($"Value '{input}' is not a valid member of enum '{actualType.Name}'.");
            }

            result = enumResult;
            return true;
        }

        if (actualType == typeof(DateTime))
        {
            // attempt to parse the string using the supported date formats
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
        // !IsValueType indicates a reference type (string, class)
        // Nullable.GetUnderlyingType != null indicates a nullable value type (int?)
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}