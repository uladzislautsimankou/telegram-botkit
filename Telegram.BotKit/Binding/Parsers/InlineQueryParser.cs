using Telegram.BotKit.Helpers;

namespace Telegram.BotKit.Binding.Parsers;

internal static class InlineQueryParser
{
    public static (string Key, List<string> Parameters) Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (string.Empty, new());

        var trimmed = input.Trim();
        var firstSpaceIndex = trimmed.IndexOf(' ');

        string key;
        string rawArgs;

        // if there are no spaces, the query consists of only the key without arguments
        if (firstSpaceIndex == -1)
        {
            key = trimmed;
            rawArgs = string.Empty;
        }
        else
        {
            key = input[0..firstSpaceIndex];            // extract the query key
            rawArgs = input[(firstSpaceIndex + 1)..];   // extract raw arguments string
        }

        var parameters = ArgumentParser.ParseArgs(rawArgs);

        return (key, parameters);
    }
}
