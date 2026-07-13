namespace Telegram.BotKit.Callbacks.Parsing;

/// <summary>
/// Utility parser for extracting keys and query parameters from raw callback query data.
/// </summary>
internal static class CallbackParser
{
    private static readonly Dictionary<string, string> EmptyParams = new(StringComparer.OrdinalIgnoreCase);

    // format: callbackKey?param1=val1&paramN=valN
    public static (string Key, Dictionary<string, string> Parameters) Parse(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return (string.Empty, EmptyParams);

        // limit the split to 2 parts: [0] = "callbackKey", [1] = "param1=val1&paramN=valN"
        var span = data.AsSpan();
        var queryIndex = span.IndexOf('?');

        var key = queryIndex < 0 ? data : span[..queryIndex].ToString();
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // if no parameters are present, return early
        if (queryIndex < 0 || queryIndex == span.Length - 1)
            return (key, parameters);

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

                var nameStr = Uri.UnescapeDataString(rawName.ToString());
                var valStr = rawValue.IsEmpty ? string.Empty : Uri.UnescapeDataString(rawValue.ToString());

                parameters[nameStr] = valStr;
            }

            if (ampIndex < 0)
                break;

            remaining = remaining[(ampIndex + 1)..];
        }

        return (key, parameters);
    }
}
