namespace Telegram.BotKit.Shared.Exceptions;

/// <summary>
/// Thrown when the raw input string cannot be parsed due to syntax errors.
/// </summary>
/// <param name="input">The raw input that could not be parsed.</param>
/// <param name="message">The message that describes the error.</param>
public sealed class InputParsingException(string input, string message)
    : BotKitException(message)
{
    /// <summary>
    /// Gets the raw input that could not be parsed.
    /// </summary>
    public string Input { get; } = input;
}

