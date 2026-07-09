using System.Collections.Concurrent;
using System.Reflection;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Binding.Binders;

internal record ParamMetadata(
    PropertyInfo PropertyInfo,
    string? Name,
    int Position,
    bool Required,
    Type PropertyType
);

internal abstract class ParameterBinderBase<TAttribute>(IEnumerable<IValueConverter> converters) 
    where TAttribute : Attribute
{
    // cache parameter metadata to avoid performing reflection lookups on every invocation
    private readonly ConcurrentDictionary<Type, List<ParamMetadata>> _cache = new();

    protected TParams BindInternal<TParams>(List<string> rawParams) where TParams : new()
    {
        // split raw arguments into a positional queue and named arguments dictionary
        var (positionalQueue, namedArgs) = SplitParams(rawParams);

        var instance = new TParams();
        var metadataList = _cache.GetOrAdd(typeof(TParams), BuildMetadata);

        foreach (var meta in metadataList.OrderBy(m => m.Position))
        {
            var rawValue = GetRawValue(meta, namedArgs, positionalQueue);

            // validate presence of required parameters
            if (rawValue is null)
            {
                if (meta.Required)
                    throw new MissingParameterException(meta.Name ?? meta.PropertyInfo.Name);

                continue;
            }

            // convert the raw string value to the target property type
            try
            {
                var conversionCtx = new ValueConversionContext(rawValue, meta.PropertyType, instance);
                var value = ConvertValue(conversionCtx);
                meta.PropertyInfo.SetValue(instance, value);
            }
            catch (Exception)
            {
                throw new InvalidParameterTypeException(
                    parameterName: meta.Name ?? meta.PropertyInfo.Name,
                    rawValue: rawValue,
                    targetType: meta.PropertyType
                );
            }
        }

        return instance;
    }

    private List<ParamMetadata> BuildMetadata(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (Prop: p, Attr: p.GetCustomAttribute<TAttribute>()))
            .Where(x => x.Attr is not null)
            // invoke the abstract mapper method implemented by concrete binders
            .Select(x => MapAttribute(x.Prop, x.Attr!))
            .ToList();
    }

    private static string? GetRawValue(ParamMetadata meta, Dictionary<string, string> named, Queue<string> positional)
    {
        // match parameter by name
        var key = meta.Name ?? meta.PropertyInfo.Name;
        if (named.TryGetValue(key, out var namedVal)) return namedVal;

        // match parameter by position
        if (meta.Position >= 0 && positional.Count > 0)
        {
            return positional.Dequeue();
        }
        return null;
    }

    private static (Queue<string> Positional, Dictionary<string, string> Named) SplitParams(List<string> rawParams)
    {
        var positional = new Queue<string>(rawParams.Count);
        var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in rawParams)
        {
            // utilize ReadOnlySpan for high performance parsing without unnecessary allocations
            var span = param.AsSpan();
            var eqIndex = span.IndexOf('=');

            if (eqIndex > 0)
            {
                var key = span.Slice(0, eqIndex).ToString();
                var val = span.Slice(eqIndex + 1).ToString();
                named[key] = val;
            }
            else
            {
                positional.Enqueue(param);
            }
        }
        return (positional, named);
    }

    private object? ConvertValue(ValueConversionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Input)) return null;

        foreach (var converter in converters.Reverse())
        {
            if (converter.TryConvert(context, out var result)) return result;
        }
        throw new NotSupportedException($"No converter found for type {context.TargetType.Name}");
    }

    protected abstract ParamMetadata MapAttribute(PropertyInfo prop, TAttribute attr);
}
