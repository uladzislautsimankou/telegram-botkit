namespace Telegram.BotKit.Binding.Parsers;

internal static class CallbackParser
{
    // format: callbackKey?param1=val1&paramN=valN
    public static (string Key, Dictionary<string, string> Parameters) Parse(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return (string.Empty, new Dictionary<string, string>());

        // limit the split to 2 parts: [0] = "callbackKey", [1] = "param1=val1&paramN=valN"
        var parts = data.Split('?', 2);
        var key = parts[0];

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // if no parameters are present, return early
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            return (key, parameters);

        foreach (var pair in parts[1].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIndex = pair.IndexOf('=');

            var rawName = eqIndex < 0 ? pair : pair[..eqIndex];
            var rawValue = eqIndex < 0 ? string.Empty : pair[(eqIndex + 1)..];

            parameters[Uri.UnescapeDataString(rawName)] = Uri.UnescapeDataString(rawValue);
        }

        return (key, parameters);
    }
}
