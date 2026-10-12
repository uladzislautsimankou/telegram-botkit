using System.Buffers;
using System.Runtime.CompilerServices;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Helpers;

internal static class ArgumentParser
{
    private const int StackallocTokenThreshold = 128;

    /// <summary>
    /// Parses a raw argument string into individual argument strings handling quotes, escapes, and whitespace in a single pass.
    /// </summary>
    /// <param name="rawArgs">The raw input argument string provided by the user.</param>
    /// <returns>A list of parsed argument strings.</returns>
    /// <exception cref="InputParsingException">Thrown when an unclosed quotation mark is detected.</exception>
    public static List<string> ParseArgs(string rawArgs)
    {
        if (string.IsNullOrWhiteSpace(rawArgs))
            return [];

        var results = new List<string>();
        var span = rawArgs.AsSpan();
        var i = 0;

        while (i < span.Length)
        {
            // skip whitespace between tokens
            while (i < span.Length && char.IsWhiteSpace(span[i]))
            {
                i++;
            }

            if (i >= span.Length)
                break;

            var tokenStart = i;
            var isComplex = false;

            // optimistic fast scanning: advance until delimiter or special character
            while (i < span.Length)
            {
                var c = span[i];

                if (IsSpecial(c))
                {
                    isComplex = true;
                    break;
                }

                if (char.IsWhiteSpace(c))
                {
                    break;
                }

                i++;
            }

            // fast path: pure argument completed without quotes or escapes
            if (!isComplex)
            {
                results.Add(rawArgs[tokenStart..i]);
                continue;
            }

            // complex path: switch to buffer and continue from the current character in the same pass
            results.Add(ParseComplexToken(rawArgs, span, tokenStart, ref i));
        }

        return results;
    }

    private static string ParseComplexToken(string rawArgs, ReadOnlySpan<char> span, int tokenStart, ref int i)
    {
        var maxTokenLength = span.Length - tokenStart;
        char[]? rented = null;
        var buffer = maxTokenLength <= StackallocTokenThreshold
            ? stackalloc char[maxTokenLength]
            : (rented = ArrayPool<char>.Shared.Rent(maxTokenLength));

        try
        {
            var bufferIndex = 0;

            // copy prefix scanned before hitting the first special character
            var prefixLength = i - tokenStart;
            if (prefixLength > 0)
            {
                span.Slice(tokenStart, prefixLength).CopyTo(buffer);
                bufferIndex = prefixLength;
            }

            var inQuotes = false;
            var isEscaped = false;

            while (i < span.Length)
            {
                var c = span[i++];

                // normalize typographical quotes
                if (IsTypographicQuote(c))
                    c = '"';

                if (isEscaped)
                {
                    buffer[bufferIndex++] = c;
                    isEscaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    isEscaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    // token delimiter reached
                    break;
                }

                buffer[bufferIndex++] = c;
            }

            if (isEscaped)
            {
                buffer[bufferIndex++] = '\\';
            }

            if (inQuotes)
            {
                throw new InputParsingException(rawArgs, "Unclosed quotation mark detected in arguments.");
            }

            return new string(buffer[..bufferIndex]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSpecial(char c) =>
        c is '"' or '\\' || IsTypographicQuote(c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTypographicQuote(char c) =>
        c is '«' or '»' or '“' or '”' or '„';
}