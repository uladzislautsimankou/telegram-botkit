using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.InlineQueries.Abstractions;

/// <summary>
/// Defines a handler responsible for processing inline queries based on the execution context.
/// </summary>
public interface IInlineQueryHandler
{
    /// <summary>
    /// Determines whether this handler can process the incoming inline query in the specified context.
    /// </summary>
    /// <param name="query">The incoming inline query.</param>
    /// <returns><see langword="true"/> if this handler can process the request; otherwise, <see langword="false"/>.</returns>
    bool CanHandle(InlineQuery query);

    /// <summary>
    /// Handles the inline query execution flow and returns a batch of search results.
    /// </summary>
    /// <param name="context">The inline query execution context containing request data.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the operation that returns an <see cref="InlineQueryBatch"/>.</returns>
    Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken = default);
}