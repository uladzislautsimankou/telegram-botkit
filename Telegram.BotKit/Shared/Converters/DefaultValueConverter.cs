using System.Collections.Concurrent;
using Telegram.BotKit.Shared.Converters.Strategies;

namespace Telegram.BotKit.Shared.Converters;

/// <summary>
/// The default value converter: converts strings, enums, dates and times, types implementing <see cref="IParsable{TSelf}"/>
/// and types that expose a <see cref="System.ComponentModel.TypeConverter"/>. Nullable types accept empty input.
/// </summary>
internal sealed class DefaultValueConverter : IValueConverter
{
    // order matters: specialized strategies go first, TypeDescriptor is the last-resort fallback
    private static readonly IConversionStrategy[] Strategies =
    [
        new EnumConversionStrategy(),
        new DateTimeConversionStrategy(),
        new DateOnlyConversionStrategy(),
        new TimeOnlyConversionStrategy(),
        new ParsableConversionStrategy(),
        new TypeDescriptorConversionStrategy()
    ];

    // the strategy for a type never changes, so it is resolved once (null means "not supported")
    private static readonly ConcurrentDictionary<Type, IConversionStrategy?> StrategyCache = new();

    private enum ConversionStatus
    {
        Success,
        EmptyForNonNullable,
        Unsupported,
        InvalidFormat
    }

    /// <inheritdoc />
    public bool CanConvert(Type targetType) =>
        targetType == typeof(string) || ResolveStrategy(GetActualType(targetType)) is not null;

    /// <inheritdoc />
    public object? Convert(ValueConversionContext context)
    {
        var status = Execute(context, out var result, out var strategy);
        var targetType = context.TargetType;

        return status switch
        {
            ConversionStatus.Success => result,
            ConversionStatus.EmptyForNonNullable =>
                throw new FormatException($"Cannot convert empty string to non-nullable type '{targetType.Name}'."),
            ConversionStatus.Unsupported =>
                throw new NotSupportedException($"No conversion strategy found for target type '{targetType.Name}'."),
            _ => throw new FormatException(strategy!.DescribeFailure(context.Input, GetActualType(targetType)))
        };
    }

    /// <inheritdoc />
    public bool TryConvert(ValueConversionContext context, out object? result) =>
        Execute(context, out result, out _) is ConversionStatus.Success;

    private static ConversionStatus Execute(
        ValueConversionContext context,
        out object? result,
        out IConversionStrategy? strategy)
    {
        result = null;
        strategy = null;

        var input = context.Input;
        var targetType = context.TargetType;

        if (targetType == typeof(string))
        {
            result = input;
            return ConversionStatus.Success;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            // empty input means "no value": fine for nullable targets (result stays null)
            return IsNullable(targetType) ? ConversionStatus.Success : ConversionStatus.EmptyForNonNullable;
        }

        var actualType = GetActualType(targetType);
        strategy = ResolveStrategy(actualType);

        if (strategy is null)
        {
            return ConversionStatus.Unsupported;
        }

        return strategy.TryConvert(input, actualType, out result)
            ? ConversionStatus.Success
            : ConversionStatus.InvalidFormat;
    }

    private static IConversionStrategy? ResolveStrategy(Type actualType) =>
        StrategyCache.GetOrAdd(actualType, static type =>
        {
            for (var i = 0; i < Strategies.Length; i++)
            {
                if (Strategies[i].CanConvert(type))
                {
                    return Strategies[i];
                }
            }

            return null;
        });

    private static Type GetActualType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
