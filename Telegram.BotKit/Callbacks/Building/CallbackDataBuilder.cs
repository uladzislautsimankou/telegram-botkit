using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Telegram.BotKit.Callbacks.Attributes;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Callbacks.Building;

/// <summary>
/// Serializes a parameter model into callback data ("key?name=value&amp;name2=value2"), the inverse of the callback binder.
/// </summary>
internal static class CallbackDataBuilder
{
    private sealed record Parameter(PropertyInfo Property, string Key, bool Required, object? DefaultValue);

    private static readonly ConcurrentDictionary<Type, Parameter[]> Cache = new();

    /// <summary>
    /// Builds callback data without checking Telegram's length limit.
    /// </summary>
    /// <typeparam name="TParams">The parameter model.</typeparam>
    /// <param name="key">The route key.</param>
    /// <param name="parameters">The parameter model to encode.</param>
    /// <param name="converters">The converters asked to format values first, the highest priority first; null for none.</param>
    /// <returns>The callback data: the route key, followed by "?" and the parameters that differ from the defaults.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the key is empty or contains '?', or a required parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the model declares the same parameter name more than once.</exception>
    public static string Build<TParams>(string key, TParams parameters, IReadOnlyList<IValueConverter>? converters = null)
        where TParams : class, new()
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var normalizedKey = NormalizeKey(key);
        var pairs = new List<string>();

        foreach (var parameter in Cache.GetOrAdd(typeof(TParams), static type => Describe(type)))
        {
            var value = parameter.Property.GetValue(parameters);

            // null means "not set". A required parameter without a value would only fail when the button is clicked
            // (MissingParameterException in the binder), so it fails here, where the mistake is made.
            if (value is null)
            {
                if (parameter.Required)
                {
                    throw new ArgumentException(
                        $"Required callback parameter '{parameter.Key}' of model '{typeof(TParams).Name}' must not be null.",
                        nameof(parameters));
                }

                continue;
            }

            // a value equal to the model default is omitted because the binder starts from a fresh instance anyway;
            // required parameters are always written, otherwise binding would fail
            if (!parameter.Required && Equals(value, parameter.DefaultValue))
            {
                continue;
            }

            pairs.Add($"{Escape(parameter.Key)}={Escape(CallbackValueFormatter.Format(value, converters))}");
        }

        return pairs.Count is 0
            ? normalizedKey
            : $"{normalizedKey}?{string.Join('&', pairs)}";
    }

    private static string NormalizeKey(string key)
    {
        var normalized = key?.Trim();

        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException("Callback key must not be empty.", nameof(key));
        }

        // '?' separates the key from parameters in callback data
        if (normalized.Contains('?'))
        {
            throw new ArgumentException($"Callback key '{normalized}' must not contain '?'.", nameof(key));
        }

        return normalized;
    }

    // keep in sync with CallbackParameterBinder.MapProperty
    private static Parameter[] Describe(Type type)
    {
        var defaults = Activator.CreateInstance(type)!;

        var parameters = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters().Length == 0)
            .Select(p =>
            {
                var attribute = p.GetCustomAttribute<CallbackParamAttribute>();
                return new Parameter(p, attribute?.Name ?? p.Name, attribute?.Required ?? false, p.GetValue(defaults));
            })
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .ToArray();

        var duplicate = parameters
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Parameter model '{type.Name}' declares the parameter name '{duplicate.Key}' more than once (names are case-insensitive).");
        }

        return parameters;
    }

    // only the characters that are structural in callback data are escaped, the parser unescapes them back;
    // escaping everything (Uri.EscapeDataString) would turn ':' into '%3A' and waste the 64-byte budget
    private static string Escape(string value)
    {
        if (value.AsSpan().IndexOfAny('%', '&', '=') < 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            switch (c)
            {
                case '%': builder.Append("%25"); break;
                case '&': builder.Append("%26"); break;
                case '=': builder.Append("%3D"); break;
                default: builder.Append(c); break;
            }
        }

        return builder.ToString();
    }
}
