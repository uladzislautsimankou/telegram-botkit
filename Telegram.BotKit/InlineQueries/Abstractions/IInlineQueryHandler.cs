using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.InlineQueries.Abstractions;

/// <summary>
/// Defines a handler responsible for processing inline queries based on the execution context.
/// </summary>
public interface IInlineQueryHandler
{
    /// <summary>
    /// Determines whether this handler can process the incoming inline query.
    /// Evaluated prior to resolving the handler from the service provider,
    /// so the decision must not depend on injected services.
    /// </summary>
    /// <param name="query">The incoming inline query.</param>
    /// <returns>True if this handler can process the query; otherwise, false.</returns>
    static abstract bool CanHandle(InlineQuery query);

    /// <summary>
    /// Gets the priority of this handler among the handlers evaluated for the same update: lower values go first, the default is 0.
    /// Handlers with equal priority keep registration order: handlers found by assembly scanning are registered
    /// ordered by full type name, handlers added with AddHandler follow in call order.
    /// Message and chosen inline result updates are processed by the first handler whose CanHandle returns true;
    /// for inline queries all matching handlers run and Order defines the order of their results.
    /// </summary>
    static virtual int Order => 0;

    /// <summary>
    /// Handles the inline query execution flow and returns a batch of search results.
    /// </summary>
    /// <param name="context">The inline query execution context containing request data.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the operation that returns an <see cref="InlineQueryBatch"/>.</returns>
    Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken = default);
}
