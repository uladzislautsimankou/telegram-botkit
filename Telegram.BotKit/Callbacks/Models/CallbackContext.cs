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
    /// Initializes a new instance of the <see cref="CallbackContext"/> class.
    /// Parses the raw query data string into a routing key and unescaped query parameter arguments.
    /// </summary>
    /// <param name="query">The raw callback query received from the Telegram Bot API.</param>
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
    /// Gets or sets the resolved callback handler invoker matched and instantiated during routing.
    /// </summary>
    internal ICallbackHandlerInvoker? MatchedInvoker { get; set; }
}
