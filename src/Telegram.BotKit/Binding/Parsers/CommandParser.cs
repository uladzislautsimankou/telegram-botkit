using System.Text.RegularExpressions;

namespace Telegram.BotKit.Binding.Parsers;

internal static class CommandParser
{
    // Regex explanation:
    // ([^\s=]+)="([^"]*)"  именованный с кавычками key="val val"   Group[1] = key, Group[2] = val val)
    // "([^"]*)"            позиционный с кавычками "val val"       Group[3] = val val)
    // (\S+)                обычный параметр val или key=val        Group[4] (или Value)
    private static readonly Regex ArgsRegex = new(@"([^\s=]+)=""([^""]*)""|""([^""]*)""|(\S+)",RegexOptions.Compiled);

    // без параметров                                           /command
    // с параметрами                                            /command param1 paramN
    // без параметров, указывая, что комманда для этого бота    /command@botUsername
    // с параметрами, указывая, что комманда для этого бота     /command@botUsername param1 paramN
    public static (string Command, string? BotUsername, List<string> Parameters) Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || input[0] != '/')
            return (string.Empty, null, new());

        // находим первый пробел
        var firstSpaceIndex = input.IndexOf(' ');

        string fullCommand;
        string rawArgs;

        // если пробелов нет, то у нас просто комманда без аргументов
        if (firstSpaceIndex == -1)
        {
            fullCommand = input[1..]; // отрезаем слеш /cmd - cmd
            rawArgs = string.Empty;
        }
        else
        {
            fullCommand = input[1..firstSpaceIndex]; // вызераем комманду /cmd arg... - cmd
            rawArgs = input[(firstSpaceIndex + 1)..]; // аругменты
        }

        // проверяем, есть ли упоминание бота
        var atIndex = fullCommand.IndexOf('@');
        var (command, bot) = atIndex < 0
            ? (fullCommand, null)
            : (fullCommand[..atIndex], fullCommand[(atIndex + 1)..]);

        var parameters = ParseArgs(rawArgs);

        return (command, bot, parameters);
    }

    private static List<string> ParseArgs(string rawArgs)
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