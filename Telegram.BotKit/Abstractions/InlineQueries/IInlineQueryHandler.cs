namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Defines a handler for a specific inline query (inline mode search) with strongly typed parameters.
/// </summary>
/// <typeparam name="TParams">The type of the model to bind the inline query parameters to.</typeparam>
public interface IInlineQueryHandler<TParams> where TParams : class, new()
{
    /// <summary>
    /// Gets the unique key (route) for this inline query handler.
    /// The router matches this key against the beginning of the inline query string.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Handles the inline query execution and returns inline query results.
    /// </summary>
    /// <param name="parameters">The strongly typed parameters parsed and bound from the inline query text.</param>
    /// <param name="context">The context containing the inline query and metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(TParams parameters, InlineQueryContext context, CancellationToken cancellationToken = default);
}