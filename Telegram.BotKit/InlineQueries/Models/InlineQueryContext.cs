using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace Telegram.BotKit.InlineQueries.Models;

/// <summary>
/// Represents the execution lifecycle context for an incoming inline query search update.
/// Encapsulates the query data and aggregates results from multiple parallel handlers.
/// </summary>
public sealed class InlineQueryContext
{
    private readonly object _sync = new();
    private readonly List<InlineQueryResult> _results = [];
    private readonly List<Exception> _exceptions = [];

    private bool _isPersonal;
    private int? _cacheTime;
    private string? _nextOffset;
    private InlineQueryResultsButton? _button;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineQueryContext"/> class.
    /// </summary>
    /// <param name="query">The incoming inline query containing the user search text and sender details.</param>
    public InlineQueryContext(InlineQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        Query = query;
    }

    /// <summary>
    /// Gets the original inline query received from the Telegram Bot API containing the search trigger and sender details.
    /// </summary>
    public InlineQuery Query { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the response should NOT be sent to Telegram.
    /// Middlewares can set this to true to suppress answering (e.g. rate-limit, auth failure).
    /// </summary>
    public bool SkipAnswer { get; set; }

    /// <summary>
    /// Gets a value indicating whether at least one handler matched and executed.
    /// </summary>
    public bool IsHandled { get; internal set; }

    /// <summary>
    /// Gets a thread-safe snapshot of the aggregated inline results accumulated from all matched handlers.
    /// </summary>
    public IReadOnlyList<InlineQueryResult> Results
    {
        get
        {
            lock (_sync)
            {
                return _results.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets a view of exceptions captured from individual handlers that failed during parallel execution.
    /// </summary>
    public IReadOnlyList<Exception> Exceptions
    {
        get
        {
            lock (_sync)
            {
                return _exceptions;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the search results are personal and cached on Telegram servers exclusively for the user.
    /// </summary>
    public bool IsPersonal
    {
        get { lock (_sync) return _isPersonal; }
    }

    /// <summary>
    /// Gets the maximum amount of time in seconds that the results of the inline query may be cached on Telegram servers.
    /// </summary>
    public int? CacheTime
    {
        get { lock (_sync) return _cacheTime; }
    }

    /// <summary>
    /// Gets the offset value used for paginated inline query results, identifying where the next page should begin.
    /// </summary>
    public string? NextOffset
    {
        get { lock (_sync) return _nextOffset; }
    }

    /// <summary>
    /// Gets the button displayed above the inline query results.
    /// </summary>
    public InlineQueryResultsButton? Button
    {
        get { lock (_sync) return _button; }
    }

    /// <summary>
    /// Appends a batch of inline results to the aggregated context in a thread-safe manner.
    /// </summary>
    /// <param name="results">The results returned by a single inline query handler.</param>
    internal void AddResults(IEnumerable<InlineQueryResult> results)
    {
        lock (_sync)
        {
            _results.AddRange(results);
        }
    }

    /// <summary>
    /// Captures an exception thrown by an individual inline query handler without aborting the rest of the pipeline.
    /// </summary>
    /// <param name="exception">The exception thrown by the handler.</param>
    internal void AddException(Exception exception)
    {
        lock (_sync)
        {
            _exceptions.Add(exception);
        }
    }

    /// <summary>
    /// Applies configuration returned by a single handler and returns true if an existing button was replaced.
    /// </summary>
    /// <param name="isPersonal">Whether the results should be cached per user.</param>
    /// <param name="cacheTime">The cache ttl in seconds, if specified.</param>
    /// <param name="nextOffset">The pagination offset, if specified.</param>
    /// <param name="button">The results button, if specified.</param>
    /// <returns>True if an existing button was replaced by a new button; otherwise, false.</returns>
    internal bool MergeConfiguration(
        bool isPersonal,
        int? cacheTime,
        string? nextOffset,
        InlineQueryResultsButton? button)
    {
        lock (_sync)
        {
            if (isPersonal)
            {
                _isPersonal = true;
            }

            if (cacheTime is not null && (_cacheTime is null || cacheTime < _cacheTime))
            {
                _cacheTime = cacheTime;
            }

            if (!string.IsNullOrEmpty(nextOffset))
            {
                _nextOffset = nextOffset;
            }

            var hasButtonConflict = button is not null && _button is not null;

            if (button is not null)
            {
                _button = button;
            }

            return hasButtonConflict;
        }
    }
}