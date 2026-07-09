using Telegram.BotKit.Helpers;

namespace Telegram.BotKit.Binding.Parsers;

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
            return (string.Empty, null, new());

        // find the first space character
        var firstSpaceIndex = input.IndexOf(' ');

        string fullCommand;
        string rawArgs;

        // if there are no spaces, the command has no arguments
        if (firstSpaceIndex == -1)
        {
            fullCommand = input[1..]; // trim the leading slash (e.g., /cmd -> cmd)
            rawArgs = string.Empty;
        }
        else
        {
            fullCommand = input[1..firstSpaceIndex]; // extract the command name (e.g., /cmd arg... -> cmd)
            rawArgs = input[(firstSpaceIndex + 1)..]; // extract raw arguments string
        }

        // check if the command targets a specific bot username (e.g., /cmd@botUsername)
        var atIndex = fullCommand.IndexOf('@');
        var (command, bot) = atIndex < 0
            ? (fullCommand, null)
            : (fullCommand[..atIndex], fullCommand[(atIndex + 1)..]);

        var parameters = ArgumentParser.ParseArgs(rawArgs);

        return (command, bot, parameters);
    }
}