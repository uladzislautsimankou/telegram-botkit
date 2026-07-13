using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Parsing;

namespace Telegram.BotKit.Commands.Models;

/// <summary>
/// Represents the execution context for an incoming text command update.
/// Encapsulates the original Telegram message, parsed triggers, bot targeting details, and bound route information.
/// </summary>
public record CommandContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// Parses the raw message text into a structured command trigger, target bot, and positional/named parameters.
    /// </summary>
    /// <param name="message">The incoming message containing the command string to process.</param>
    public CommandContext(Message message)
    {
        Message = message;
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
    /// Gets the primary command trigger name extracted from the message text (e.g., "start" for /start).
    /// This trigger does not include the leading slash or any targeted bot usernames.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the username of the target bot explicitly mentioned in the command string (e.g., "MyBot" in /start@MyBot).
    /// Returns null if the command was invoked without an explicit bot mention.
    /// </summary>
    public string? TargetBotUsername { get; }

    /// <summary>
    /// Gets the list of raw string arguments parsed from the portion of the command following the trigger.
    /// Can contain both positional values and key-value named parameters.
    /// </summary>
    public List<string> RawParams { get; }

    /// <summary>
    /// Gets or sets a virtual route key used for handler matching and custom routing logic.
    /// When assigned, routing middlewares will evaluate this value for handler dispatching instead of the default command.
    /// </summary>
    public string? VirtualRoute { get; set; }

    /// <summary>
    /// Gets or sets the matched command metadata containing details about the handler instance and its resolved interface type.
    /// </summary>
    public CommandMetadata? MatchedCommandMetadata { get; set; }

    /// <summary>
    /// Gets or sets the matched handler invoker instance responsible for binding and executing the target command flow.
    /// </summary>
    internal ICommandHandlerInvoker? MatchedInvoker { get; set; }
}