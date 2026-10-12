namespace Telegram.BotKit.Commands.Parsing;

internal static class CommandParser
{
    // format examples:
    //   without parameters:                                    /command
    //   with parameters:                                       /command param1 paramN
    //   without parameters, targeting a specific bot:          /command@botUsername
    //   with parameters, targeting a specific bot:             /command@botUsername param1 paramN
    public static (string Command, string? BotUsername, string RawArgs) ParseHeader(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || input[0] != '/')
            return (string.Empty, null, string.Empty);

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
        var rawArgs = string.Empty;

        // if there are no spaces, the command has no arguments
        if (firstWhitespaceIndex == -1)
        {
            fullCommandSpan = span[1..]; // trim the leading slash (e.g., /cmd -> cmd)
        }
        else
        {
            fullCommandSpan = span[1..firstWhitespaceIndex]; // extract the command name (e.g., /cmd arg... -> cmd)
            rawArgs = span[(firstWhitespaceIndex + 1)..].ToString(); // extract raw arguments string
        }

        if (fullCommandSpan.IsEmpty)
            return (string.Empty, null, string.Empty);

        // check if the command targets a specific bot username (e.g., /cmd@botUsername)
        var atIndex = fullCommandSpan.IndexOf('@');
        if (atIndex < 0)
        {
            return (fullCommandSpan.ToString(), null, rawArgs);
        }

        return (
            fullCommandSpan[..atIndex].ToString(),
            fullCommandSpan[(atIndex + 1)..].ToString(),
            rawArgs
        );
    }
}