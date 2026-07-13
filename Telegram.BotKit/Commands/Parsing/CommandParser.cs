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
        var firstSpaceIndex = span.IndexOf(' ');

        ReadOnlySpan<char> fullCommandSpan;
        string rawArgs;

        // if there are no spaces, the command has no arguments
        if (firstSpaceIndex == -1)
        {
            fullCommandSpan = span[1..]; // trim the leading slash (e.g., /cmd -> cmd)
            rawArgs = string.Empty;
        }
        else
        {
            fullCommandSpan = span[1..firstSpaceIndex]; // extract the command name (e.g., /cmd arg... -> cmd)
            rawArgs = span[(firstSpaceIndex + 1)..].ToString(); // extract raw arguments string
        }

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