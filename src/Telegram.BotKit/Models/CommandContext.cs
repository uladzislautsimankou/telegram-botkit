using Telegram.Bot.Types;
using Telegram.BotKit.Binding.Parsers;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit;

/// <summary>
/// Represents the context of an incoming text command.
/// Encapsulates the original message and the parsed command arguments.
/// </summary>
public record CommandContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// Parses the message text into a command trigger and a list of arguments.
    /// </summary>
    /// <param name="message">The message received from Telegram.</param>
    /// <exception cref="MissingParameterException">Thrown if the message is null or does not contain a Sender (From).</exception>
    public CommandContext(Message message)
    {
        Message = message ?? throw new MissingParameterException(nameof(message));
        From = message.From ?? throw new MissingParameterException(nameof(message.From));

        var parsed = CommandParser.Parse(message.Text);
        Command = parsed.Command;
        TargetBotUsername = parsed.BotUsername;
        RawParams = parsed.Parameters;
    }

    /// <summary>
    /// Gets the original message received from Telegram.
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// Gets the user who sent the command.
    /// </summary>
    public User From { get; }

    /// <summary>
    /// Gets the command name without the leading slash (e.g., "start" for /start).
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the username of the bot explicitly mentioned in the command (e.g., "MyBot" in /start@MyBot).
    /// Null if the command was sent without a mention.
    /// </summary>
    public string? TargetBotUsername { get; }

    /// <summary>
    /// Gets the list of raw string arguments passed after the command.
    /// </summary>
    public List<string> RawParams { get; }
}