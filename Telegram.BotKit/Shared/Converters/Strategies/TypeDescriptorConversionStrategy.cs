using System.ComponentModel;

namespace Telegram.BotKit.Shared.Converters.Strategies;

internal sealed class TypeDescriptorConversionStrategy : IConversionStrategy
{
    public bool CanConvert(Type targetType)
    {
        var converter = TypeDescriptor.GetConverter(targetType);
        return converter is not null && converter.CanConvertFrom(typeof(string));
    }

    public object Convert(string input, Type targetType)
    {
        var converter = TypeDescriptor.GetConverter(targetType);

        try
        {
            if (IsFloatingPoint(targetType))
            {
                input = input.Replace(',', '.');
            }

            return converter.ConvertFromInvariantString(input)!;
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to convert value '{input}' to type '{targetType.Name}'.", ex);
        }
    }

    private static bool IsFloatingPoint(Type type) =>
        type == typeof(double) || type == typeof(float) || type == typeof(decimal);
}