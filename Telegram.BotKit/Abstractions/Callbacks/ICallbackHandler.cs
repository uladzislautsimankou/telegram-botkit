namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Defines a handler for a specific callback query (inline button interaction) with strongly typed parameters.
/// </summary>
/// <typeparam name="TParams">The type of the model to bind the callback data parameters to.</typeparam>
public interface ICallbackHandler<TParams> where TParams : class, new()
{
    /// <summary>
    /// Gets the unique key (route) for this callback handler.
    /// The router matches this key against the beginning of the callback data string (e.g., "game:move").
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Handles the callback query execution.
    /// </summary>
    /// <param name="parameters">The strongly typed parameters parsed and bound from the callback data.</param>
    /// <param name="context">The context containing the callback query and metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(TParams parameters, CallbackContext context, CancellationToken cancellationToken = default);
}
