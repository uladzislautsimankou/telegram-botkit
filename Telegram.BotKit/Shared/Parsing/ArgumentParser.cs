using System.Text.RegularExpressions;

namespace Telegram.BotKit.Helpers;

internal static partial class ArgumentParser
{
    // regex explanation:
    // ([^\s=]+)="([^"]*)"  named parameter with quotes key="val val" (Group[1] = key, Group[2] = val val)
    // "([^"]*)"            positional parameter with quotes "val val" (Group[3] = val val)
    // (\S+)                standard parameter val or key=val (Group[4] or raw Value)
    [GeneratedRegex(@"([^\s=]+)=""([^""]*)""|""([^""]*)""|(\S+)")]
    private static partial Regex ArgsRegex();

    public static List<string> ParseArgs(string rawArgs)
    {
        if (string.IsNullOrWhiteSpace(rawArgs))
            return new();

        var matches = ArgsRegex().Matches(rawArgs);
        var results = new List<string>(matches.Count);

        foreach (Match m in matches)
        {
            // if the argument is a named parameter with quotes: key="val val"
            // extract the value from the quotes and format it back as key=val val
            if (m.Groups[1].Success)
            {
                results.Add($"{m.Groups[1].Value}={m.Groups[2].Value}");
                continue;
            }

            // if it is a positional parameter with quotes: "val val"
            // extract the inner unquoted string
            if (m.Groups[3].Success)
            {
                results.Add(m.Groups[3].Value);
                continue;
            }

            // otherwise, add the entire matched plain word (e.g., val or key=val)
            results.Add(m.Value);
        }

        return results;
    }
}