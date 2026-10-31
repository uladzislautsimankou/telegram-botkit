using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Parsing;
using Telegram.BotKit.Shared.Parsing;

namespace Telegram.BotKit.Commands.Models;

/// <summary>
/// Represents the execution context for an incoming text command update.
/// </summary>
public sealed class CommandContext
{
    private readonly string _rawArgs;
    private List<string>? _rawParams;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="message">The incoming message containing the command string to process.</param>
    public CommandContext(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Message = message;
        var header = CommandParser.ParseHeader(message.Text);
        Command = header.Command;
        TargetBotUsername = header.BotUsername;
        _rawArgs = header.RawArgs;
    }

    /// <summary>
    /// Gets the original message received from Telegram.
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// Gets the primary command trigger name extracted from the message text.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the username of the target bot explicitly mentioned in the command string.
    /// </summary>
    public string? TargetBotUsername { get; }

    /// <summary>
    /// Gets or sets a virtual route key used for custom handler matching.
    /// </summary>
    public string? VirtualRoute { get; set; }

    /// <summary>
    /// Gets or sets the result of evaluating the command route during the routing phase.
    /// </summary>
    public CommandRouteResult RouteResult { get; set; }

    /// <summary>
    /// Gets the arguments following the command, split by whitespace. Double and typographic quotes («», “”, „“) group
    /// words into one argument; a backslash escapes the next character everywhere (also outside quotes),
    /// so write <c>\\</c> for a literal backslash. An unclosed quote throws
    /// <see cref="Telegram.BotKit.Shared.Exceptions.InputParsingException"/> on first access.
    /// Parsed lazily, only when a handler with bindable parameters needs the arguments.
    /// </summary>
    public List<string> RawParams => _rawParams ??= ArgumentParser.ParseArgs(_rawArgs);

    /// <summary>
    /// Gets or sets the matched command metadata containing details about the handler instance and its resolved type.
    /// </summary>
    public CommandMetadata? MatchedCommandMetadata { get; set; }

    /// <summary>
    /// Gets or sets the matched command registration containing compiled factory and invoker delegates.
    /// </summary>
    internal CommandRegistration? MatchedRegistration { get; set; }
}
