using Telegram.BotKit.Callbacks.Models;

namespace Telegram.BotKit.Callbacks.Abstractions;

/// <summary>
/// Defines a handler for a specific callback query (inline button interaction) with strongly typed parameters.
/// The framework does not answer the callback query itself: call <c>AnswerCallbackQuery</c> in the handler
/// or in a middleware, otherwise the button keeps showing the loading indicator.
/// </summary>
/// <typeparam name="TParams">The type of the model to bind the callback data parameters to.</typeparam>
public interface ICallbackHandler<TParams> where TParams : class, new()
{
    /// <summary>
    /// Gets the unique route key of this handler.
    /// The router compares it, ignoring case, with the part of the callback data before the first '?'
    /// (e.g. "game:move" for "game:move?x=1&amp;y=2"); the rest is bound to <typeparamref name="TParams"/>.
    /// The key must not contain '?'.
    /// </summary>
    static abstract string Key { get; }

    /// <summary>
    /// Handles the callback query execution.
    /// </summary>
    /// <param name="parameters">The strongly typed parameters parsed and bound from the callback data.</param>
    /// <param name="context">The context containing the callback query and metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(TParams parameters, CallbackContext context, CancellationToken cancellationToken = default);
}
