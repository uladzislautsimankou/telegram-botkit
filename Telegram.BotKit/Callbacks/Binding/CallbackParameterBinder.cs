using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Callbacks.Binding;

/// <summary>
/// Binds incoming callback parameters to strongly-typed models using registered value converters.
/// </summary>
internal sealed class CallbackParameterBinder : ICallbackParameterBinder
{
    // cache properties of target models to avoid expensive reflection lookups on every callback
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _cache = new();
    private readonly IValueConverter[] _reversedConverters;

    public CallbackParameterBinder(IEnumerable<IValueConverter> converters)
    {
        var list = new List<IValueConverter>(converters);
        list.Reverse();
        _reversedConverters = list.ToArray();
    }

    public TParams Bind<TParams>(CallbackContext context) where TParams : class, new()
    {
        var instance = new TParams();

        if (context.RawParams.Count == 0)
            return instance;

        var propertyMap = _cache.GetOrAdd(typeof(TParams), BuildPropertyMap);

        foreach (var (key, rawValue) in context.RawParams)
        {
            if (propertyMap.TryGetValue(key, out var prop))
            {
                try
                {
                    var value = ConvertValue(rawValue, prop.PropertyType);
                    prop.SetValue(instance, value);
                }
                catch (Exception)
                {
                    throw new InvalidParameterTypeException(
                        parameterName: key,
                        rawValue: rawValue,
                        targetType: prop.PropertyType
                    );
                }
            }
        }

        return instance;
    }

    private static Dictionary<string, PropertyInfo> BuildPropertyMap(Type type)
    {
        var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite) continue;

            var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();

            // use the JsonPropertyNameAttribute override if present; fallback to property name otherwise
            var key = attr?.Name ?? prop.Name;

            if (!map.ContainsKey(key))
            {
                map[key] = prop;
            }
        }

        return map;
    }

    private object? ConvertValue(string? input, Type type)
    {
        // delegate empty string and whitespace handling directly to converters
        foreach (var converter in _reversedConverters)
        {
            if (converter.TryConvert(input, type, out var result))
            {
                return result;
            }
        }

        throw new NotSupportedException($"No converter found for type {type.Name}");
    }
}
