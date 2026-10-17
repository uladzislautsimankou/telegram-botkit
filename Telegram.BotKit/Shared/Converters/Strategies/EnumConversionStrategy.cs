using System.Collections.Concurrent;

namespace Telegram.BotKit.Shared.Converters.Strategies;

/// <summary>
/// Converts a member name or a number to an enum value, ignoring case. Undefined values are rejected; flags may be combined.
/// </summary>
internal sealed class EnumConversionStrategy : IConversionStrategy
{
    private sealed record EnumMetadata(bool IsFlags, ulong Bitmask);

    private static readonly ConcurrentDictionary<Type, EnumMetadata> MetadataCache = new();

    /// <inheritdoc />
    public bool CanConvert(Type targetType) => targetType.IsEnum;

    /// <inheritdoc />
    public bool TryConvert(string input, Type targetType, out object? result)
    {
        result = null;

        if (!Enum.TryParse(targetType, input, ignoreCase: true, out var parsed))
        {
            return false;
        }

        var metadata = MetadataCache.GetOrAdd(targetType, static type => CreateMetadata(type));

        // Enum.TryParse accepts any number, so the value must still be a defined member or a combination of defined flags
        var isValid = metadata.IsFlags
            ? (ToUInt64(parsed) & ~metadata.Bitmask) == 0
            : Enum.IsDefined(targetType, parsed);

        if (!isValid)
        {
            return false;
        }

        result = parsed;
        return true;
    }

    /// <inheritdoc />
    public string DescribeFailure(string input, Type targetType) =>
        targetType.IsDefined(typeof(FlagsAttribute), inherit: false)
            ? $"Value '{input}' is not a valid member or combination of flags for enum '{targetType.Name}'."
            : $"Value '{input}' is not a valid member of enum '{targetType.Name}'.";

    private static EnumMetadata CreateMetadata(Type type)
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
