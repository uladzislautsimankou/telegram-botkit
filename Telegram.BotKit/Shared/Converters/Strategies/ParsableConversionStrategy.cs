using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Telegram.BotKit.Shared.Converters.Strategies;

/// <summary>
/// Converts to any type implementing <see cref="IParsable{TSelf}"/> (numbers, bool, Guid, TimeSpan, Version, ...)
/// without exceptions for malformed input. The typed TryParse delegate is built once per type.
/// </summary>
internal sealed class ParsableConversionStrategy : IConversionStrategy
{
    private delegate bool TryParseDelegate(string input, out object? result);

    private static readonly MethodInfo CreateParserMethod = typeof(ParsableConversionStrategy)
        .GetMethod(nameof(CreateParser), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, TryParseDelegate?> ParserCache = new();

    /// <inheritdoc />
    public bool CanConvert(Type targetType) => GetParser(targetType) is not null;

    /// <inheritdoc />
    public bool TryConvert(string input, Type targetType, out object? result)
    {
        var parser = GetParser(targetType);
        if (parser is null)
        {
            result = null;
            return false;
        }

        // users commonly type a decimal comma; invariant parsing would otherwise read it as a thousands separator
        if (IsFloatingPoint(targetType))
        {
            input = input.Replace(',', '.');
        }

        return parser(input, out result);
    }

    private static TryParseDelegate? GetParser(Type type) =>
        ParserCache.GetOrAdd(type, static t => ImplementsParsable(t)
            ? (TryParseDelegate)CreateParserMethod.MakeGenericMethod(t).Invoke(null, null)!
            : null);

    private static bool ImplementsParsable(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType
            && i.GetGenericTypeDefinition() == typeof(IParsable<>)
            && i.GenericTypeArguments[0] == type);

    private static TryParseDelegate CreateParser<T>() where T : IParsable<T> =>
        static (string input, out object? result) =>
        {
            if (T.TryParse(input, CultureInfo.InvariantCulture, out var value))
            {
                result = value;
                return true;
            }

            result = null;
            return false;
        };

    private static bool IsFloatingPoint(Type type) =>
        type == typeof(double) || type == typeof(float) || type == typeof(decimal);
}
