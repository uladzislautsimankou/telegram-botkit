using System.Collections.Concurrent;
using System.Reflection;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Shared.Binding;

/// <summary>
/// Metadata representing properties to bind.
/// </summary>
internal record ParamMetadata(
    PropertyInfo PropertyInfo,
    string? Name,
    int Position,
    bool Required,
    Type PropertyType
);

/// <summary>
/// Base class providing capability of binding raw inputs into strongly-typed parameter models.
/// </summary>
internal abstract class ParameterBinderBase<TAttribute>(IEnumerable<IValueConverter> converters) 
    where TAttribute : Attribute
{
    // cache parameter metadata to avoid performing reflection lookups on every invocation
    private readonly ConcurrentDictionary<Type, List<ParamMetadata>> _cache = new();
    private readonly IValueConverter[] _reversedConverters = converters.Reverse().ToArray();

    protected TParams BindInternal<TParams>(List<string> rawParams) where TParams : new()
    {
        // split raw arguments into a positional queue and named arguments dictionary
        var (positionalQueue, namedArgs) = SplitParams(rawParams);

        var instance = new TParams();
        var metadataList = _cache.GetOrAdd(typeof(TParams), BuildMetadata);

        foreach (var meta in metadataList)
        {
            var rawValue = GetRawValue(meta, namedArgs, positionalQueue, metadataList);

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
            .OrderBy(m => m.Position)
            .ToList();
    }

    private static string? GetRawValue(
        ParamMetadata meta,
        Dictionary<string, string> named,
        Queue<string> positional,
        List<ParamMetadata> allMetadata)
    {
        // match parameter by name
        var key = meta.Name ?? meta.PropertyInfo.Name;
        if (named.TryGetValue(key, out var namedVal)) return namedVal;

        // match parameter by position
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

        foreach (var param in rawParams)
        {
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

        foreach (var converter in _reversedConverters)
        {
            if (converter.TryConvert(context, out var result)) return result;
        }
        throw new NotSupportedException($"No converter found for type {context.TargetType.Name}");
    }

    /// <summary>
    /// Maps property metadata and its custom attribute definition to internal parameter metadata.
    /// </summary>
    protected abstract ParamMetadata MapAttribute(PropertyInfo prop, TAttribute attr);
}
