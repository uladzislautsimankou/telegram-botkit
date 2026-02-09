using Telegram.Bot.Types;
using Telegram.BotKit.Binding.Parsers;

namespace Telegram.BotKit;

/// <summary>
/// Represents the context of an incoming inline query from Telegram's inline mode.
/// Encapsulates the original inline query and the parsed routing data.
/// </summary>
public record InlineQueryContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InlineQueryContext"/> class.
    /// Parses the inline query text into a routing key and a list of parameters.
    /// </summary>
    /// <param name="query">The inline query received from Telegram.</param>
    public InlineQueryContext(InlineQuery query)
    {
        Id = query.Id;
        From = query.From;
        Query = query.Query;

        var parsedData = InlineQueryParser.Parse(query.Query);

        Key = parsedData.Key;
        RawParams = parsedData.Parameters;
    }

    /// <summary>
    /// Gets the unique identifier of the inline query.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets the user who sent the inline query.
    /// </summary>
    public User From { get; }

    /// <summary>
    /// Gets the original query text sent by the user in inline mode.
    /// </summary>
    public string Query { get; set; }

    /// <summary>
    /// Gets the routing key extracted from the inline query (e.g., "search", "gif").
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the list of raw parameters parsed from the inline query text.
    /// </summary>
    public List<string> RawParams { get; }
}
