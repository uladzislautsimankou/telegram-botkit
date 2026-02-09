using System.Text.RegularExpressions;

namespace Telegram.BotKit.Helpers;

internal static class ArgumentParser
{
    // Regex explanation:
    // ([^\s=]+)="([^"]*)"  именованный с кавычками key="val val"   Group[1] = key, Group[2] = val val)
    // "([^"]*)"            позиционный с кавычками "val val"       Group[3] = val val)
    // (\S+)                обычный параметр val или key=val        Group[4] (или Value)
    private static readonly Regex ArgsRegex = new(@"([^\s=]+)=""([^""]*)""|""([^""]*)""|(\S+)", RegexOptions.Compiled);

    public static List<string> ParseArgs(string rawArgs)
    {
        if (string.IsNullOrWhiteSpace(rawArgs))
            return new();

        return ArgsRegex.Matches(rawArgs)
            .Select(m =>
            {
                // если key="val val"
                // вырезаем из кавычек и склеиваем обратно в key=val val
                if (m.Groups[1].Success)
                    return $"{m.Groups[1].Value}={m.Groups[2].Value}";

                // если просто кавычки
                if (m.Groups[3].Success)
                    return m.Groups[3].Value;

                // Иначе берем просто слово
                return m.Value;
            })
            .ToList();
    }
}
