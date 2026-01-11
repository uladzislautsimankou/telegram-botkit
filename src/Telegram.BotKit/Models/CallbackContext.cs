using Telegram.Bot.Types;
using Telegram.BotKit.Binding.Parsers;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit;

/// <summary>
/// Represents the context of an incoming callback query (inline keyboard interaction).
/// Encapsulates the original query and the parsed routing data.
/// </summary>
public record CallbackContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackContext"/> class.
    /// Parses the callback data into a routing key and a dictionary of parameters.
    /// </summary>
    /// <param name="query">The callback query received from Telegram.</param>
    /// <exception cref="MissingParameterException">Thrown if the query does not contain a Message or a Sender (From).</exception>
    public CallbackContext(CallbackQuery query)
    {
        Query = query;
        Message = query.Message ?? throw new MissingParameterException(nameof(query.Message));
        From = query.From ?? throw new MissingParameterException(nameof(query.From));

        var parsedData = CallbackParser.Parse(query.Data);

        Key = parsedData.Key;
        RawParams = parsedData.Parameters;
    }

    /// <summary>
    /// Gets the original callback query received from Telegram.
    /// </summary>
    public CallbackQuery Query { get; }

    /// <summary>
    /// Gets the message to which the inline keyboard is attached.
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// Gets the user who triggered the callback query.
    /// </summary>
    public User From { get; }

    /// <summary>
    /// Gets the routing key extracted from the callback data (e.g., "game:move").
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the raw key-value parameters parsed from the callback data.
    /// </summary>
    public Dictionary<string, string> RawParams { get; }
}
