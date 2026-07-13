using Telegram.BotKit.Callbacks.Models;

namespace Telegram.BotKit.Callbacks.Binding;

/// <summary>
/// Defines a binder responsible for mapping incoming callback query parameters to a strongly typed model.
/// </summary>
internal interface ICallbackParameterBinder
{
    /// <summary>
    /// Binds callback query parameters from the context to a new instance of <typeparamref name="TParams"/>.
    /// </summary>
    /// <typeparam name="TParams">The type of the parameter model to bind to.</typeparam>
    /// <param name="context">The callback query execution context.</param>
    /// <returns>A populated instance of the parameter model.</returns>
    TParams Bind<TParams>(CallbackContext context) where TParams : class, new();
}
