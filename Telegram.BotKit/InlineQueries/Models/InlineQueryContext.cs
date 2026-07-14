using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace Telegram.BotKit.InlineQueries.Models;

/// <summary>
/// Represents the execution lifecycle context for an incoming inline query search update.
/// Encapsulates the query data, the scoped service container, and aggregates results from multiple parallel handlers.
/// </summary>
/// <param name="query">The incoming inline query containing the user query text.</param>
/// <param name="serviceProvider">The scoped service provider resolved for the current request lifecycle.</param>
public sealed class InlineQueryContext(
    InlineQuery query,
    IServiceProvider serviceProvider)
{
    /// <summary>
    /// Gets the original inline query received from the Telegram Bot API containing the search trigger and sender details.
    /// </summary>
    public InlineQuery Query { get; } = query;

    /// <summary>
    /// Gets the scoped dependency injection container allocated for the duration of this inline update request.
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    /// <summary>
    /// Gets the aggregated collection of inline results accumulated from all matched inline handlers.
    /// </summary>
    public List<InlineQueryResult> Results { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the search results are personal and cached on the server side 
    /// exclusively for the user who sent the query. Default is false (cached globally).
    /// </summary>
    public bool IsPersonal { get; set; }

    /// <summary>
    /// Gets or sets the maximum amount of time in seconds that the results of the inline query may be cached on Telegram servers.
    /// </summary>
    public int? CacheTime { get; set; }

    /// <summary>
    /// Gets or sets the offset value used for paginated inline query results, identifying where the next page should begin.
    /// </summary>
    public string? NextOffset { get; set; }

    /// <summary>
    /// Gets or sets the button displayed above the inline query results.
    /// </summary>
    public InlineQueryResultsButton? Button { get; set; }

    /// <summary>
    /// Gets the collection of exceptions captured from individual handlers that failed during parallel execution.
    /// </summary>
    public List<Exception> Exceptions { get; } = [];
}