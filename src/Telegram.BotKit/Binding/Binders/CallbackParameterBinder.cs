using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using Telegram.BotKit.Binding.Converters;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Binding.Binders;

internal sealed class CallbackParameterBinder : ICallbackParameterBinder
{
    // кэш, что бы каждый раз не дергать рефлексию на кадждый коллбэк
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _cache = new();

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
                    var value = ValueConverter.Convert(rawValue, prop.PropertyType);
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

            // если атрибут есть, то берем имя оттуда. если нет, то берем имя свойства
            var key = attr?.Name ?? prop.Name;

            if (!map.ContainsKey(key))
            {
                map[key] = prop;
            }
        }

        return map;
    }
}
