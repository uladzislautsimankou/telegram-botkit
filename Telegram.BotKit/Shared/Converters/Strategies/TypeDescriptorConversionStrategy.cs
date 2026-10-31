using System.Collections.Concurrent;
using System.ComponentModel;

namespace Telegram.BotKit.Shared.Converters.Strategies;

/// <summary>
/// Last-resort strategy for types that only expose a <see cref="TypeConverter"/> (e.g. <see cref="Uri"/>).
/// </summary>
internal sealed class TypeDescriptorConversionStrategy : IConversionStrategy
{
    private static readonly ConcurrentDictionary<Type, TypeConverter?> ConverterCache = new();

    /// <inheritdoc />
    public bool CanConvert(Type targetType) => GetConverter(targetType) is not null;

    /// <inheritdoc />
    public bool TryConvert(string input, Type targetType, out object? result)
    {
        result = null;

        var converter = GetConverter(targetType);
        if (converter is null)
        {
            return false;
        }

        try
        {
            result = converter.ConvertFromInvariantString(input);
            return result is not null;
        }
        catch (Exception)
        {
            // TypeConverter has no non-throwing API and wraps parse errors in arbitrary exception types
            result = null;
            return false;
        }
    }

    private static TypeConverter? GetConverter(Type type) =>
        ConverterCache.GetOrAdd(type, static t =>
        {
            var converter = TypeDescriptor.GetConverter(t);
            return converter.CanConvertFrom(typeof(string)) ? converter : null;
        });
}
