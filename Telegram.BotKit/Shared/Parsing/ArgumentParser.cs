using System.Text;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Helpers;

internal static class ArgumentParser
{
    /// <summary>
    /// Parses a raw argument string into a list of individual arguments while handling quotations,
    /// escape sequences, and key-value bindings.
    /// </summary>
    /// <remarks>
    /// Supported formats include plain positional tokens such as value,
    /// standard named parameters such as key=value,
    /// quoted positional tokens with spaces such as "value with spaces" or «value with spaces»,
    /// and quoted named parameters such as key="value with spaces".
    /// </remarks>
    /// <param name="rawArgs">The raw input argument string provided by the user.</param>
    /// <returns>A list of parsed argument strings.</returns>
    /// <exception cref="InputParsingException">Thrown when an unclosed quotation mark is detected.</exception>
    public static List<string> ParseArgs(string rawArgs)
    {
        if (string.IsNullOrWhiteSpace(rawArgs))
            return [];

        var results = new List<string>();
        var current = new StringBuilder(rawArgs.Length);
        var inQuotes = false;
        var isEscaped = false;
        var hasToken = false;

        for (var i = 0; i < rawArgs.Length; i++)
        {
            var c = rawArgs[i];

            // normalize typographical and mobile quotation marks to standard double quotes
            if (c is '«' or '»' or '“' or '”' or '„')
                c = '"';

            // handle escaped characters such as escaped quotes or backslashes
            if (isEscaped)
            {
                current.Append(c);
                isEscaped = false;
                hasToken = true;
                continue;
            }

            if (c == '\\')
            {
                isEscaped = true;
                continue;
            }

            // toggle quoted string state without appending the quote delimiter itself
            if (c == '"')
            {
                inQuotes = !inQuotes;
                hasToken = true;
                continue;
            }

            // word delimiter outside of quotation marks
            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (hasToken)
                {
                    results.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }
                continue;
            }

            current.Append(c);
            hasToken = true;
        }

        // handle trailing backslash if present at the end of input
        if (isEscaped)
        {
            current.Append('\\');
            hasToken = true;
        }

        if (inQuotes)
        {
            throw new InputParsingException(rawArgs, "Unclosed quotation mark detected in arguments.");
        }

        if (hasToken)
        {
            results.Add(current.ToString());
        }

        return results;
    }
}