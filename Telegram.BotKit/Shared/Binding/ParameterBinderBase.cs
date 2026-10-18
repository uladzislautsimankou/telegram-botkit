using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Shared.Binding;

/// <summary>
/// Base class providing capability of binding raw inputs into strongly-typed parameter models.
/// Derived binders decide which properties are parameters and where the raw values come from.
/// </summary>
/// <param name="converters">The registered value converters; the one registered last has the highest priority.</param>
internal abstract class ParameterBinderBase(IEnumerable<IValueConverter> converters) : IParameterModelValidator
{
    private readonly ConcurrentDictionary<Type, ParameterBindingPlan> _cache = new();
    private readonly IValueConverter[] _reversedConverters = converters.Reverse().ToArray();

    /// <summary>
    /// Gets the cached binding plan of a parameter model, building it on first use.
    /// </summary>
    /// <typeparam name="TParams">The parameter model type.</typeparam>
    /// <returns>The binding plan of the model.</returns>
    protected ParameterBindingPlan GetPlan<TParams>() => GetPlan(typeof(TParams));

    private ParameterBindingPlan GetPlan(Type type) =>
        _cache.GetOrAdd(type, static (t, self) => self.BuildPlan(t), this);

    /// <summary>
    /// Builds the binding plan of a parameter model and checks it, so that mistakes in the model surface at startup
    /// instead of at the first update that uses it.
    /// </summary>
    /// <param name="modelType">The parameter model type.</param>
    /// <returns>A description of every problem found; empty when the model is valid.</returns>
    public IReadOnlyList<string> Validate(Type modelType)
    {
        ParameterBindingPlan plan;
        try
        {
            plan = GetPlan(modelType);
        }
        catch (InvalidOperationException ex)
        {
            return [ex.Message];
        }
        catch (Exception ex)
        {
            return [$"Parameter model '{modelType.Name}' could not be analysed: {ex.GetType().Name}: {ex.Message}"];
        }

        List<string>? problems = null;

        foreach (var meta in plan.Params)
        {
            // ResolveConverter falls back to a converter that does not support the type when nothing matches
            if (!meta.Converter.CanConvert(meta.PropertyType))
            {
                (problems ??= []).Add(
                    $"Property '{modelType.Name}.{meta.PropertyInfo.Name}' has type '{meta.PropertyType.Name}', " +
                    "which no registered IValueConverter supports. Change the type or register a custom IValueConverter.");
            }
        }

        return problems ?? [];
    }

    /// <summary>
    /// Creates the model and fills it from the already split raw values.
    /// </summary>
    /// <typeparam name="TParams">The parameter model type.</typeparam>
    /// <param name="plan">The binding plan of the model.</param>
    /// <param name="positional">Positional values in input order, or null when the input has none (e.g. callback data).</param>
    /// <param name="named">Named values, looked up case-insensitively.</param>
    /// <returns>A populated instance of the parameter model.</returns>
    /// <exception cref="MissingParameterException">Thrown when a required parameter has no value.</exception>
    /// <exception cref="InvalidParameterTypeException">Thrown when a value cannot be converted to the property type.</exception>
    protected static TParams BindCore<TParams>(
        ParameterBindingPlan plan,
        Queue<string>? positional,
        IReadOnlyDictionary<string, string> named) where TParams : new()
    {
        var instance = new TParams();
        var parameters = plan.Params;

        for (var i = 0; i < parameters.Length; i++)
        {
            var meta = parameters[i];
            var rawValue = GetRawValue(meta, named, positional);

            if (rawValue is null)
            {
                if (meta.Required)
                {
                    throw new MissingParameterException(meta.Key);
                }

                continue;
            }

            var conversionCtx = new ValueConversionContext(rawValue, meta.PropertyType, instance);
            if (!meta.Converter.TryConvert(conversionCtx, out var value))
            {
                throw new InvalidParameterTypeException(
                    parameterName: meta.Key,
                    rawValue: rawValue,
                    targetType: meta.PropertyType);
            }

            meta.Setter(instance, value);
        }

        return instance;
    }

    /// <summary>
    /// Splits command arguments into positional values and "name=value" pairs.
    /// A token "x=y" is a named argument only when "x" is a known parameter name, otherwise it stays positional.
    /// </summary>
    /// <param name="rawParams">The parsed command arguments.</param>
    /// <param name="namedKeys">The names that may be supplied in the "name=value" form.</param>
    /// <returns>The positional values in input order and the named values, looked up case-insensitively.</returns>
    protected static (Queue<string> Positional, Dictionary<string, string> Named) SplitParams(
        List<string> rawParams,
        FrozenSet<string> namedKeys)
    {
        var positional = new Queue<string>(rawParams.Count);
        var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rawParams.Count; i++)
        {
            var param = rawParams[i];
            var eqIndex = param.IndexOf('=');

            if (eqIndex > 0)
            {
                var name = param[..eqIndex];
                if (namedKeys.Contains(name))
                {
                    named[name] = param[(eqIndex + 1)..];
                    continue;
                }
            }

            positional.Enqueue(param);
        }

        return (positional, named);
    }

    /// <summary>
    /// Maps a property to its binding metadata, or returns null when the property is not a parameter.
    /// </summary>
    /// <param name="property">A public writable instance property of the parameter model.</param>
    /// <param name="resolveConverter">Resolves the value converter for a property type.</param>
    /// <returns>The binding metadata of the property, or null when the property is not a parameter.</returns>
    protected abstract ParamMetadata? MapProperty(PropertyInfo property, Func<Type, IValueConverter> resolveConverter);

    private ParameterBindingPlan BuildPlan(Type type)
    {
        var bindable = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
            .Select(p => MapProperty(p, ResolveConverter))
            .OfType<ParamMetadata>()
            .ToList();

        ValidateModel(type, bindable);

        // positions define binding order only: gaps are fine, duplicates were rejected above
        var ordered = bindable.OrderBy(m => m.Position).ToArray();

        // the last positional string swallows the remaining positional values
        var lastPositional = Array.FindLastIndex(ordered, m => m.IsPositional);
        if (lastPositional >= 0 && ordered[lastPositional].PropertyType == typeof(string))
        {
            ordered[lastPositional] = ordered[lastPositional] with { TakesRemainder = true };
        }

        var keys = ordered
            .Select(m => m.Key)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        return new ParameterBindingPlan(ordered, keys);
    }

    private static void ValidateModel(Type type, List<ParamMetadata> parameters)
    {
        var duplicateName = parameters
            .GroupBy(m => m.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateName is not null)
        {
            throw new InvalidOperationException(
                $"Parameter model '{type.Name}' declares the parameter name '{duplicateName.Key}' more than once (names are case-insensitive).");
        }

        var duplicatePosition = parameters
            .Where(m => m.IsPositional)
            .GroupBy(m => m.Position)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicatePosition is not null)
        {
            var owners = string.Join(", ", duplicatePosition.Select(m => m.PropertyInfo.Name));
            throw new InvalidOperationException(
                $"Parameter model '{type.Name}' assigns Position {duplicatePosition.Key} to several properties ({owners}). " +
                "Positions define the binding order and must be unique.");
        }
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

        return _reversedConverters.Length > 0
            ? _reversedConverters[0]
            : throw new InvalidOperationException("No IValueConverter is registered.");
    }

    private static string? GetRawValue(
        ParamMetadata meta,
        IReadOnlyDictionary<string, string> named,
        Queue<string>? positional)
    {
        if (named.TryGetValue(meta.Key, out var namedValue))
        {
            return namedValue;
        }

        if (!meta.IsPositional || positional is null || positional.Count == 0)
        {
            return null;
        }

        if (!meta.TakesRemainder || positional.Count == 1)
        {
            return positional.Dequeue();
        }

        var parts = new string[positional.Count];
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = positional.Dequeue();
        }

        return string.Join(' ', parts);
    }
}

