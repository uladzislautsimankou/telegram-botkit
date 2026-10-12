using Telegram.Bot.Types.InlineQueryResults;

namespace Telegram.BotKit.InlineQueries.Models;

/// <summary>
/// Represents a batch of results returned by an individual inline query handler.
/// </summary>
public sealed record InlineQueryBatch
{
    public InlineQueryBatch() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineQueryBatch"/> class with the specified results and configuration options.
    /// </summary>
    /// <param name="results">The collection of inline results returned by the handler.</param>
    /// <param name="button">The optional button displayed above the inline query results.</param>
    /// <param name="isPersonal">A value indicating whether the results should be cached exclusively for the user who sent the query.</param>
    /// <param name="cacheTime">The maximum amount of time in seconds that the results may be cached on Telegram servers.</param>
    /// <param name="nextOffset">The offset value identifying where the next page of results should begin.</param>
    public InlineQueryBatch(
        IReadOnlyList<InlineQueryResult> results,
        InlineQueryResultsButton? button = null,
        bool isPersonal = false,
        int? cacheTime = null,
        string? nextOffset = null)
    {
        Results = results;
        Button = button;
        IsPersonal = isPersonal;
        CacheTime = cacheTime;
        NextOffset = nextOffset;
    }

    /// <summary>
    /// Gets the aggregated collection of inline results accumulated from all matched inline handlers.
    /// </summary>
    public IReadOnlyList<InlineQueryResult> Results { get; init; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the search results are personal and cached on the server side 
    /// exclusively for the user who sent the query. Default is false (cached globally).
    /// </summary>
    public bool IsPersonal { get; init; }

    /// <summary>
    /// Gets or sets the maximum amount of time in seconds that the results of the inline query may be cached on Telegram servers.
    /// </summary>
    public int? CacheTime { get; init; }

    /// <summary>
    /// Gets or sets the offset value used for paginated inline query results, identifying where the next page should begin.
    /// </summary>
    public string? NextOffset { get; init; }

    /// <summary>
    /// Gets or sets the button displayed above the inline query results.
    /// </summary>
    public InlineQueryResultsButton? Button { get; init; }
}