using System.Text.RegularExpressions;

namespace Telegram.BotKit.Helpers;

internal static class ArgumentParser
{
    // regex explanation:
    // ([^\s=]+)="([^"]*)"  named parameter with quotes key="val val" (Group[1] = key, Group[2] = val val)
    // "([^"]*)"            positional parameter with quotes "val val" (Group[3] = val val)
    // (\S+)                standard parameter val or key=val (Group[4] or raw Value)
    private static readonly Regex ArgsRegex = new(@"([^\s=]+)=""([^""]*)""|""([^""]*)""|(\S+)", RegexOptions.Compiled);

    public static List<string> ParseArgs(string rawArgs)
    {
        if (string.IsNullOrWhiteSpace(rawArgs))
            return new();

        return ArgsRegex.Matches(rawArgs)
            .Select(m =>
            {
                // if the argument is a named parameter with quotes: key="val val"
                // extract the value from the quotes and format it back as key=val val
                if (m.Groups[1].Success)
                    return $"{m.Groups[1].Value}={m.Groups[2].Value}";

                // if it is a positional parameter with quotes: "val val"
                // extract the inner unquoted string
                if (m.Groups[3].Success)
                    return m.Groups[3].Value;

                // otherwise, add the entire matched plain word (e.g., val or key=val)
                return m.Value;
            })
            .ToList();
    }
}
