using System.Collections.Frozen;

namespace Telegram.BotKit.Callbacks.Parsing;

/// <summary>
/// Parses raw callback query data ("key?name=value&amp;name2=value2") into a route key and parameters.
/// Works on spans, so only the resulting key, names and values are allocated.
/// </summary>
internal static class CallbackParser
{
    private static readonly FrozenDictionary<string, string> EmptyParams = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Parses callback query data into a route key and its parameters.
    /// </summary>
    /// <param name="data">The raw callback data; null or whitespace yields an empty key.</param>
    /// <returns>The route key, trimmed, and the parameters by name (names are case-insensitive).</returns>
    public static (string Key, IReadOnlyDictionary<string, string> Parameters) Parse(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return (string.Empty, EmptyParams);
        }

        var span = data.AsSpan();
        var queryIndex = span.IndexOf('?');

        // the registry and the data builder trim keys, so data written by hand with stray whitespace around the key
        // still matches; data without any whitespace to trim is not copied
        var keySpan = (queryIndex < 0 ? span : span[..queryIndex]).Trim();
        var key = keySpan.Length == data.Length ? data : keySpan.ToString();

        // return early with frozen singleton when no query parameters are present
        if (queryIndex < 0 || queryIndex == span.Length - 1)
        {
            return (key, EmptyParams);
        }

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var remaining = span[(queryIndex + 1)..];

        while (!remaining.IsEmpty)
        {
            var ampIndex = remaining.IndexOf('&');
            var pair = ampIndex < 0 ? remaining : remaining[..ampIndex];

            if (!pair.IsEmpty)
            {
                var eqIndex = pair.IndexOf('=');
                var rawName = eqIndex < 0 ? pair : pair[..eqIndex];
                var rawValue = eqIndex < 0 ? default : pair[(eqIndex + 1)..];

                if (!rawName.IsEmpty)
                {
                    var nameStr = UnescapeFast(rawName);
                    var valStr = rawValue.IsEmpty ? string.Empty : UnescapeFast(rawValue);

                    parameters[nameStr] = valStr;
                }
            }

            if (ampIndex < 0)
            {
                break;
            }

            remaining = remaining[(ampIndex + 1)..];
        }

        return (key, parameters);
    }

    private static string UnescapeFast(ReadOnlySpan<char> span)
    {
        // skip expensive unescaping if no percent characters exist in the span
        if (span.IndexOf('%') < 0)
        {
            return span.ToString();
        }

        return Uri.UnescapeDataString(span.ToString());
    }
}
