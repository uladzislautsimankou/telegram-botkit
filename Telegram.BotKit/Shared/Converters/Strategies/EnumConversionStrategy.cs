using System.Collections.Concurrent;

namespace Telegram.BotKit.Shared.Converters.Strategies;

internal sealed class EnumConversionStrategy : IConversionStrategy
{
    private sealed record EnumMetadata(bool IsFlags, ulong Bitmask);

    private static readonly ConcurrentDictionary<Type, EnumMetadata> MetadataCache = new();

    public bool CanConvert(Type targetType) => targetType.IsEnum;

    public object Convert(string input, Type targetType)
    {
        if (!Enum.TryParse(targetType, input, ignoreCase: true, out var enumResult))
        {
            throw new FormatException($"Value '{input}' is not a valid member of enum '{targetType.Name}'.");
        }

        var metadata = MetadataCache.GetOrAdd(targetType, static type =>
        {
            var isFlags = type.IsDefined(typeof(FlagsAttribute), inherit: false);
            ulong mask = 0;

            if (isFlags)
            {
                foreach (var val in Enum.GetValues(type))
                {
                    mask |= ToUInt64(val);
                }
            }

            return new EnumMetadata(isFlags, mask);
        });

        if (!metadata.IsFlags)
        {
            if (!Enum.IsDefined(targetType, enumResult))
            {
                throw new FormatException($"Value '{input}' is not a valid member of enum '{targetType.Name}'.");
            }

            return enumResult;
        }

        var bits = ToUInt64(enumResult);
        if ((bits & ~metadata.Bitmask) != 0)
        {
            throw new FormatException($"Value '{input}' is not a valid member or combination of flags for enum '{targetType.Name}'.");
        }

        return enumResult;
    }

    private static ulong ToUInt64(object val) => Type.GetTypeCode(val.GetType()) switch
    {
        TypeCode.SByte => (byte)(sbyte)val,
        TypeCode.Byte => (byte)val,
        TypeCode.Int16 => (ushort)(short)val,
        TypeCode.UInt16 => (ushort)val,
        TypeCode.Int32 => (uint)(int)val,
        TypeCode.UInt32 => (uint)val,
        TypeCode.Int64 => (ulong)(long)val,
        TypeCode.UInt64 => (ulong)val,
        _ => System.Convert.ToUInt64(val)
    };
}
