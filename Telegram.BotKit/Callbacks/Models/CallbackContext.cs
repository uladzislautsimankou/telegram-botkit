using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Parsing;

namespace Telegram.BotKit.Callbacks.Models;

/// <summary>
/// Represents the execution context for an incoming callback query resulting from inline keyboard button interactions.
/// </summary>
public sealed class CallbackContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackContext"/> class by parsing the raw data of a callback query.
    /// </summary>
    /// <param name="query">The callback query received from Telegram.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null.</exception>
    public CallbackContext(CallbackQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        Query = query;

        var parsedData = CallbackParser.Parse(query.Data);

        Key = parsedData.Key;
        RawParams = parsedData.Parameters;
    }

    /// <summary>
    /// Gets the original callback query object containing the message context, click sender, and query metadata.
    /// </summary>
    public CallbackQuery Query { get; }

    /// <summary>
    /// Gets the primary routing key parsed from the beginning of the callback query data.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the collection of raw parameter arguments parsed from the query string format of the callback data.
    /// </summary>
    public IReadOnlyDictionary<string, string> RawParams { get; }

    /// <summary>
    /// Gets or sets the matched callback metadata containing details about the handler instance and its resolved type.
    /// It is null when no handler is registered for <see cref="Key"/>, for example for a button of a message that was
    /// sent before a deployment renamed or removed the handler. The pipeline then continues (a warning is logged),
    /// so middleware placed after routing can react, e.g. answer the callback query with a notice.
    /// </summary>
    public CallbackMetadata? MatchedCallbackMetadata { get; set; }

    /// <summary>
    /// Gets or sets the matched callback registration containing compiled factory and invoker delegates.
    /// </summary>
    internal CallbackRegistration? MatchedRegistration { get; set; }
}


