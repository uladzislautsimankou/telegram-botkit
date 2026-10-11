using Telegram.BotKit.Helpers;

namespace Telegram.BotKit.Commands.Parsing;

internal static class CommandParser
{
    // format examples:
    //   without parameters:                                    /command
    //   with parameters:                                       /command param1 paramN
    //   without parameters, targeting a specific bot:          /command@botUsername
    //   with parameters, targeting a specific bot:             /command@botUsername param1 paramN
    public static (string Command, string? BotUsername, List<string> Parameters) Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || input[0] != '/')
            return (string.Empty, null, []);

        // find the first space character
        var span = input.AsSpan();
        var firstWhitespaceIndex = -1;
        for (var i = 1; i < span.Length; i++)
        {
            if (char.IsWhiteSpace(span[i]))
            {
                firstWhitespaceIndex = i;
                break;
            }
        }

        ReadOnlySpan<char> fullCommandSpan;
        string rawArgs;

        // if there are no spaces, the command has no arguments
        if (firstWhitespaceIndex == -1)
        {
            fullCommandSpan = span[1..]; // trim the leading slash (e.g., /cmd -> cmd)
            rawArgs = string.Empty;
        }
        else
        {
            fullCommandSpan = span[1..firstWhitespaceIndex]; // extract the command name (e.g., /cmd arg... -> cmd)
            rawArgs = span[(firstWhitespaceIndex + 1)..].ToString(); // extract raw arguments string
        }

        if (fullCommandSpan.IsEmpty)
            return (string.Empty, null, []);

        // check if the command targets a specific bot username (e.g., /cmd@botUsername)
        var atIndex = fullCommandSpan.IndexOf('@');
        string command;
        string? bot = null;

        if (atIndex < 0)
        {
            command = fullCommandSpan.ToString();
        }
        else
        {
            command = fullCommandSpan[..atIndex].ToString();
            bot = fullCommandSpan[(atIndex + 1)..].ToString();
        }

        var parameters = ArgumentParser.ParseArgs(rawArgs);

        return (command, bot, parameters);
    }
}