using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Parsing;
using Telegram.BotKit.WidgetQueries.Invocation;

namespace Telegram.BotKit.WidgetQueries.Models;

/// <summary>
/// Represents the execution context for an incoming widget query: a tap on a widget that asks the bot.
/// It is one context for both kinds of the Bot API update: a query from an original message (<see cref="Query"/>)
/// and a query from a forwarded copy of a shared widget (<see cref="SharedQuery"/>).
/// </summary>
public sealed class WidgetQueryContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetQueryContext"/> class from a query of an original message,
    /// parsing its data.
    /// </summary>
    /// <param name="query">The widget query received from Telegram.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null.</exception>
    public WidgetQueryContext(WidgetQuery query)
        : this(
            (query ?? throw new ArgumentNullException(nameof(query))).Id,
            query.From,
            query.Data,
            query.WidgetId,
            query.Type,
            original: query,
            shared: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetQueryContext"/> class from a query of a forwarded copy,
    /// parsing its data.
    /// </summary>
    /// <param name="query">The shared widget query received from Telegram.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null.</exception>
    public WidgetQueryContext(SharedWidgetQuery query)
        : this(
            (query ?? throw new ArgumentNullException(nameof(query))).Id,
            query.From,
            query.Data,
            query.WidgetId,
            query.Type,
            original: null,
            shared: query)
    {
    }

    private WidgetQueryContext(
        string id,
        User from,
        string data,
        string? widgetId,
        WidgetQueryType type,
        WidgetQuery? original,
        SharedWidgetQuery? shared)
    {
        Id = id;
        From = from;
        Data = data;
        WidgetId = widgetId;
        QueryType = type;
        Query = original;
        SharedQuery = shared;

        // the data of a widget query has the format of callback data ("key?name=value"), so the callback parser is reused
        var parsedData = CallbackParser.Parse(data);

        Key = parsedData.Key;
        RawParams = parsedData.Parameters;
    }

    /// <summary>
    /// Gets the query of an original message; null when the query comes from a forwarded copy (<see cref="IsCopy"/>).
    /// </summary>
    public WidgetQuery? Query { get; }

    /// <summary>
    /// Gets the query of a forwarded copy of a shared widget; null when the query comes from an original message.
    /// </summary>
    public SharedWidgetQuery? SharedQuery { get; }

    /// <summary>
    /// Gets a value indicating whether the widget was tapped in a forwarded copy. The bot then knows nothing
    /// about the message and the chat: <see cref="Chat"/>, <see cref="MessageId"/> and <see cref="InlineMessageId"/> are null.
    /// </summary>
    public bool IsCopy => SharedQuery is not null;

    /// <summary>
    /// Gets the unique identifier of the query, the one <c>answerWidgetQuery</c> takes.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the user who tapped the widget.
    /// </summary>
    public User From { get; }

    /// <summary>
    /// Gets the raw data the widget sent with the request.
    /// </summary>
    public string Data { get; }

    /// <summary>
    /// Gets the identifier of the shared widget; null for a static widget. It is always set for a copy.
    /// </summary>
    public string? WidgetId { get; }

    /// <summary>
    /// Gets the type of the query as the Bot API describes it (data, data signed by a TON wallet of the user,
    /// or a notification about a transaction sent from it), with the wallet details of that variant.
    /// The type does not take part in routing: <see cref="Data"/> is the data of the query of every type,
    /// so all of them are routed by <see cref="Key"/>.
    /// </summary>
    public WidgetQueryType QueryType { get; }

    /// <summary>
    /// Gets the chat of the tapped message; null for a copy and for a message sent via the bot.
    /// </summary>
    public Chat? Chat => Query?.Chat;

    /// <summary>
    /// Gets the identifier of the tapped message in <see cref="Chat"/>; null for a copy and for a message sent via the bot.
    /// </summary>
    public int? MessageId => Query?.MessageId;

    /// <summary>
    /// Gets the identifier of the tapped message sent via the bot; null otherwise.
    /// </summary>
    public string? InlineMessageId => Query?.InlineMessageId;

    /// <summary>
    /// Gets the primary routing key parsed from the beginning of the query data.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the collection of raw parameter arguments parsed from the query string format of the query data.
    /// </summary>
    public IReadOnlyDictionary<string, string> RawParams { get; }

    /// <summary>
    /// Gets or sets the matched widget query metadata containing details about the handler instance and its resolved type.
    /// It is null when no handler is registered for <see cref="Key"/>, for example for a widget sent before a deployment
    /// renamed or removed the handler. The pipeline then continues (a warning is logged), so middleware placed after
    /// routing can react, e.g. send an empty answer instead of letting the widget wait for the timeout.
    /// </summary>
    public WidgetQueryMetadata? MatchedWidgetQueryMetadata { get; set; }

    /// <summary>
    /// Gets or sets the matched widget query registration containing compiled factory and invoker delegates.
    /// </summary>
    internal WidgetQueryRegistration? MatchedRegistration { get; set; }
}
