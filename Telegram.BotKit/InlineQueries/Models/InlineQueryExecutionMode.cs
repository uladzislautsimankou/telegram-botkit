namespace Telegram.BotKit.InlineQueries.Models;

/// <summary>
/// Specifies how multiple matched inline query handlers are executed.
/// </summary>
public enum InlineQueryExecutionMode
{
    /// <summary>
    /// Matched handlers run concurrently, each in its own dependency scope, so scoped services
    /// (e.g. a DbContext) are never shared between threads. Scoped state populated by earlier
    /// middleware is not visible to handlers in this mode; use <see cref="Sequential"/> if handlers need it.
    /// </summary>
    Parallel,

    /// <summary>
    /// Matched handlers run one after another within the request scope.
    /// </summary>
    Sequential
}
