using System.Collections.Concurrent;
using System.Reflection;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Shared.Binding;

/// <summary>
/// Metadata representing properties to bind along with compiled setter delegates.
/// </summary>
internal record ParamMetadata(
    PropertyInfo PropertyInfo,
    string? Name,
    int Position,
    bool Required,
    Type PropertyType,
    IValueConverter Converter,
    Action<object, object?> Setter
);

/// <summary>
/// Base class providing capability of binding raw inputs into strongly-typed parameter models.
/// </summary>
internal abstract class ParameterBinderBase<TAttribute>(IEnumerable<IValueConverter> converters)
    where TAttribute : Attribute
{
    private readonly ConcurrentDictionary<Type, List<ParamMetadata>> _cache = new();
    private readonly IValueConverter[] _reversedConverters = converters.Reverse().ToArray();

    protected TParams BindInternal<TParams>(List<string> rawParams) where TParams : new()
    {
        var (positionalQueue, namedArgs) = SplitParams(rawParams);

        var instance = new TParams();
        var metadataList = _cache.GetOrAdd(typeof(TParams), BuildMetadata);

        for (var i = 0; i < metadataList.Count; i++)
        {
            var meta = metadataList[i];
            var rawValue = GetRawValue(meta, namedArgs, positionalQueue, metadataList);

            if (rawValue is null)
            {
                if (meta.Required)
                    throw new MissingParameterException(meta.Name ?? meta.PropertyInfo.Name);

                continue;
            }

            var conversionCtx = new ValueConversionContext(rawValue, meta.PropertyType, instance);
            if (!meta.Converter.TryConvert(conversionCtx, out var value))
            {
                throw new InvalidParameterTypeException(
                    parameterName: meta.Name ?? meta.PropertyInfo.Name,
                    rawValue: rawValue,
                    targetType: meta.PropertyType
                );
            }

            meta.Setter(instance, value);
        }

        return instance;
    }

    private List<ParamMetadata> BuildMetadata(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p => (Prop: p, Attr: p.GetCustomAttribute<TAttribute>()))
            .Where(x => x.Attr is not null)
            .Select(x =>
            {
                var meta = MapAttribute(x.Prop, x.Attr!);
                var converter = ResolveConverter(meta.PropertyType);
                return meta with { Converter = converter };
            })
            .OrderBy(m => m.Position)
            .ToList();
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

    private static string? GetRawValue(
        ParamMetadata meta,
        Dictionary<string, string> named,
        Queue<string> positional,
        List<ParamMetadata> allMetadata)
    {
        var key = meta.Name ?? meta.PropertyInfo.Name;
        if (named.TryGetValue(key, out var namedVal)) return namedVal;

        if (meta.Position >= 0 && positional.Count > 0)
        {
            var isLastPositionalString = meta.PropertyType == typeof(string)
                && !allMetadata.Any(m => m.Position > meta.Position && m.Position >= 0);

            if (isLastPositionalString)
            {
                var remaining = new List<string>(positional.Count);
                while (positional.Count > 0)
                {
                    remaining.Add(positional.Dequeue());
                }
                return string.Join(" ", remaining);
            }

            return positional.Dequeue();
        }
        return null;
    }

    private static (Queue<string> Positional, Dictionary<string, string> Named) SplitParams(List<string> rawParams)
    {
        var positional = new Queue<string>(rawParams.Count);
        var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rawParams.Count; i++)
        {
            var param = rawParams[i];
            var span = param.AsSpan();
            var eqIndex = span.IndexOf('=');

            if (eqIndex > 0)
            {
                named[span[..eqIndex].ToString()] = span[(eqIndex + 1)..].ToString();
            }
            else
            {
                positional.Enqueue(param);
            }
        }
        return (positional, named);
    }

    /// <summary>
    /// Maps property metadata and its custom attribute definition to internal parameter metadata.
    /// </summary>
    protected abstract ParamMetadata MapAttribute(PropertyInfo prop, TAttribute attr);
}