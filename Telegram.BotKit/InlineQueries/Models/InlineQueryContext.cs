using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace Telegram.BotKit.InlineQueries.Models;

/// <summary>
/// Represents the execution lifecycle context for an incoming inline query search update.
/// Encapsulates the query data and aggregates results from multiple parallel handlers.
/// </summary>
/// <remarks>
/// <para>
/// This context is created once per incoming <see cref="InlineQuery"/> and is scoped to the
/// middleware pipeline execution for that single update.
/// </para>
/// <para>
/// Handlers may run in parallel (see <c>InlineQueryRoutingMiddleware</c>), so all mutating
/// operations on <see cref="Results"/> and <see cref="Exceptions"/> are internally synchronized.
/// Public read access returns a snapshot to prevent callers from observing inconsistent state.
/// </para>
/// </remarks>
/// <param name="query">The incoming inline query containing the user's search text and sender details.</param>
public sealed record InlineQueryContext(InlineQuery query)
{
    // Sync root for all mutable state in this context.
    // Never lock on a publicly exposed object (like a List<T>) - it can lead to deadlocks
    // and doesn't communicate intent. A dedicated private object is the correct pattern.
    private readonly object _sync = new();

    // Backing fields. Handlers run in parallel, so all writes must go through the
    // internal Add* methods below, which synchronize on _sync.
    private readonly List<InlineQueryResult> _results = [];
    private readonly List<Exception> _exceptions = [];

    // Mutable scalar properties. These are written by handlers during pipeline execution
    // and read by the dispatcher after the pipeline completes.
    // Access is protected by the same lock to keep the context internally consistent.
    private bool _isPersonal;
    private int? _cacheTime;
    private string? _nextOffset;
    private InlineQueryResultsButton? _button;

    /// <summary>
    /// Gets the original inline query received from the Telegram Bot API
    /// containing the search trigger and sender details.
    /// </summary>
    public InlineQuery Query { get; } = query;

    /// <summary>
    /// Gets a snapshot of the aggregated inline results accumulated from all matched handlers.
    /// </summary>
    /// <remarks>
    /// Returns a copy of the internal collection to avoid exposing mutable state to callers.
    /// Read once after the pipeline completes (typically in the dispatcher).
    /// </remarks>
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
    /// Gets a snapshot of exceptions captured from individual handlers
    /// that failed during (possibly parallel) execution.
    /// </summary>
    /// <remarks>
    /// A handler failure does not abort the pipeline - other handlers continue to run.
    /// The dispatcher may inspect this collection to log or aggregate errors.
    /// </remarks>
    public IReadOnlyList<Exception> Exceptions
    {
        get
        {
            lock (_sync)
            {
                return _exceptions.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the search results are personal and cached
    /// on Telegram servers exclusively for the user who sent the query.
    /// Default is <see langword="false"/> (cached globally).
    /// </summary>
    public bool IsPersonal
    {
        get { lock (_sync) return _isPersonal; }
    }

    /// <summary>
    /// Gets the maximum amount of time in seconds that the results of the inline query
    /// may be cached on Telegram servers.
    /// </summary>
    public int? CacheTime
    {
        get { lock (_sync) return _cacheTime; }
    }

    /// <summary>
    /// Gets the offset value used for paginated inline query results,
    /// identifying where the next page should begin.
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
    /// Appends a batch of inline results to the aggregated context.
    /// </summary>
    /// <param name="results">The results returned by a single inline query handler.</param>
    /// <remarks>
    /// Internal: only the routing middleware should call this during pipeline execution.
    /// </remarks>
    internal void AddResults(IEnumerable<InlineQueryResult> results)
    {
        lock (_sync)
        {
            _results.AddRange(results);
        }
    }

    /// <summary>
    /// Captures an exception thrown by an individual inline query handler
    /// without aborting the rest of the pipeline.
    /// </summary>
    /// <param name="exception">The exception thrown by the handler.</param>
    /// <remarks>
    /// Internal: only the routing middleware should call this during pipeline execution.
    /// </remarks>
    internal void AddException(Exception exception)
    {
        lock (_sync)
        {
            _exceptions.Add(exception);
        }
    }

    /// <summary>
    /// Applies configuration returned by a single inline query handler to the aggregated context.
    /// </summary>
    /// <param name="isPersonal">Whether the results should be cached per-user.</param>
    /// <param name="cacheTime">The cache TTL in seconds, if specified.</param>
    /// <param name="nextOffset">The pagination offset, if specified.</param>
    /// <param name="button">The results button, if specified.</param>
    /// <remarks>
    /// <para>
    /// Merging rules:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><paramref name="isPersonal"/> - OR across handlers (any <c>true</c> wins).</description></item>
    ///   <item><description><paramref name="cacheTime"/> - MIN across handlers (shortest TTL wins for safety).</description></item>
    ///   <item><description><paramref name="nextOffset"/> - last non-null value wins.</description></item>
    ///   <item><description><paramref name="button"/> - last non-null value wins; conflicts are logged by the middleware.</description></item>
    /// </list>
    /// </remarks>
    internal void MergeConfiguration(
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

            if (button is not null)
            {
                _button = button;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a button has already been assigned by a previous handler.
    /// Used by the routing middleware to detect conflicts and log a warning.
    /// </summary>
    internal bool HasButton
    {
        get { lock (_sync) return _button is not null; }
    }
}