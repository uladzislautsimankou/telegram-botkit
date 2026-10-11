namespace Telegram.BotKit.Shared.Converters.Strategies;

internal sealed class EnumConversionStrategy : IConversionStrategy
{
    public bool CanConvert(Type targetType) => targetType.IsEnum;

    public object Convert(string input, Type targetType)
    {
        if (!Enum.TryParse(targetType, input, ignoreCase: true, out var enumResult))
        {
            throw new FormatException($"Value '{input}' is not a valid member of enum '{targetType.Name}'.");
        }

        // standard enums require a strictly defined member to reject phantom numeric casts (e.g., "999")
        if (!targetType.IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            if (!Enum.IsDefined(targetType, enumResult))
            {
                throw new FormatException($"Value '{input}' is not a valid member of enum '{targetType.Name}'.");
            }

            return enumResult;
        }

        // flags enums allow bitwise combinations
        // validate against the aggregate bitmask of declared values
        ulong mask = 0;
        foreach (var val in Enum.GetValues(targetType))
        {
            mask |= ToUInt64(val);
        }

        ulong bits = ToUInt64(enumResult);
        if ((bits & ~mask) != 0)
        {
            throw new FormatException($"Value '{input}' is not a valid member or combination of flags for enum '{targetType.Name}'.");
        }

        return enumResult;
    }

    // prevents sign-extension when widening signed underlying integer types to ulong
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