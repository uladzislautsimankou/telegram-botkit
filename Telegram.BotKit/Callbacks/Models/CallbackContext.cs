using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Parsing;

namespace Telegram.BotKit.Callbacks.Models;

/// <summary>
/// Represents the execution context for an incoming callback query resulting from inline keyboard button interactions.
/// </summary>
public record CallbackContext
{
    /// <summary>
    /// Initializes a new instance of the callback context by parsing raw callback query data.
    /// </summary>
    public CallbackContext(CallbackQuery query)
    {
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
    /// Gets the primary routing key parsed from the beginning of the callback query data (e.g., "game:move" from "game:move?id=5").
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the collection of raw, unescaped parameter arguments parsed from the query string format of the callback data.
    /// </summary>
    public Dictionary<string, string> RawParams { get; }

    /// <summary>
    /// Gets or sets the matched callback metadata containing details about the handler instance and its resolved type.
    /// </summary>
    public CallbackMetadata? MatchedCallbackMetadata { get; set; }

    /// <summary>
    /// Gets or sets the resolved callback handler invoker matched and instantiated during routing.
    /// </summary>
    internal ICallbackHandlerInvoker? MatchedInvoker { get; set; }
}
