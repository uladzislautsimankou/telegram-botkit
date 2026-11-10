using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.WidgetQueries.Abstractions;

/// <summary>
/// Defines a handler for a specific widget query with strongly typed parameters.
/// The framework does not answer the query itself: call <c>answerWidgetQuery</c> in the handler or in a middleware,
/// otherwise the widget waits for 15 seconds and gets a timeout. An empty answer is enough; an answer carries only
/// a short text for the widget that asked and never changes the widget's state: for that, the bot edits it
/// (<c>editMessageWidgetData</c>, <c>editSharedWidgetData</c>).
/// </summary>
/// <remarks>
/// Queries of both kinds are routed by the same key. A query from a forwarded copy of a shared widget has no message
/// and no chat (<see cref="WidgetQueryContext.IsCopy"/> is true): a handler that reads
/// <see cref="WidgetQueryContext.Chat"/> or <see cref="WidgetQueryContext.MessageId"/> has to check it first.
/// </remarks>
/// <typeparam name="TParams">The type of the model to bind the query data parameters to.</typeparam>
public interface IWidgetQueryHandler<TParams> where TParams : class, new()
{
    /// <summary>
    /// Gets the unique route key of this handler.
    /// The router compares it, ignoring case, with the part of the query data before the first '?'
    /// (e.g. "poll:vote" for "poll:vote?o=2"); the rest is bound to <typeparamref name="TParams"/>.
    /// The key must not contain '?'.
    /// </summary>
    static abstract string Key { get; }

    /// <summary>
    /// Handles the widget query execution.
    /// </summary>
    /// <param name="parameters">The strongly typed parameters parsed and bound from the query data.</param>
    /// <param name="context">The context containing the widget query and metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(TParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default);
}
