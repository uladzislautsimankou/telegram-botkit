using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Binding;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Callbacks.Binding;

/// <summary>
/// Binds incoming callback parameters to strongly-typed models using compiled setters and value converters.
/// </summary>
internal sealed class CallbackParameterBinder(
    IEnumerable<IValueConverter> converters) : ICallbackParameterBinder
{
    private sealed record CallbackPropertyMetadata(
        Type PropertyType,
        IValueConverter Converter,
        Action<object, object?> Setter
    );

    private readonly ConcurrentDictionary<Type, Dictionary<string, CallbackPropertyMetadata>> _cache = new();
    private readonly IValueConverter[] _reversedConverters = converters.Reverse().ToArray();

    public TParams Bind<TParams>(CallbackContext context) where TParams : class, new()
    {
        var instance = new TParams();

        if (context.RawParams.Count == 0)
        {
            return instance;
        }

        var propertyMap = _cache.GetOrAdd(typeof(TParams), BuildPropertyMap);

        foreach (var (key, rawValue) in context.RawParams)
        {
            if (propertyMap.TryGetValue(key, out var propMeta))
            {
                var conversionCtx = new ValueConversionContext(rawValue, propMeta.PropertyType, instance);
                if (!propMeta.Converter.TryConvert(conversionCtx, out var value))
                {
                    throw new InvalidParameterTypeException(
                        parameterName: key,
                        rawValue: rawValue,
                        targetType: propMeta.PropertyType
                    );
                }

                propMeta.Setter(instance, value);
            }
        }

        return instance;
    }

    private Dictionary<string, CallbackPropertyMetadata> BuildPropertyMap(Type type)
    {
        var map = new Dictionary<string, CallbackPropertyMetadata>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite)
            {
                continue;
            }

            var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            var key = attr?.Name ?? prop.Name;

            if (!map.ContainsKey(key))
            {
                map[key] = new CallbackPropertyMetadata(
                    PropertyType: prop.PropertyType,
                    Converter: ResolveConverter(prop.PropertyType),
                    Setter: PropertySetterCompiler.Compile(prop)
                );
            }
        }

        return map;
    }

    private IValueConverter ResolveConverter(Type targetType)
    {
        for (var i = 0; i < _reversedConverters.Length; i++)
        {
            var converter = _reversedConverters[i];
            if (converter.CanConvert(targetType))
            {
                return converter;
            }
        }

        return _reversedConverters[0];
    }
}