using System.Text.RegularExpressions;

namespace Telegram.BotKit.Binding.Parsers;

internal static class CommandParser
{
    // ищем либо (что-то в кавычках), либо (любые символы кроме пробелов)
    private static readonly Regex ArgsRegex = new(@"\""([^\""]*)\""|(\S+)", RegexOptions.Compiled);

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

        // Match.Groups[1] - это то, что внутри кавычек (без самих кавычек)
        // Match.Groups[2] - это обычное слово
        // Value - это всё совпадение целиком (с кавычками)
        return ArgsRegex.Matches(rawArgs)
            .Select(m =>
            {
                // Если сработала первая группа (кавычки) - берем её содержимое
                if (m.Groups[1].Success) return m.Groups[1].Value;

                // Иначе берем просто слово
                return m.Value;
            })
            .ToList();
    }
}