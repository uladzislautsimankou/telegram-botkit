namespace Telegram.BotKit.Binding.Binders;

/// <summary>
/// Defines a binder responsible for mapping incoming inline query parameters to a strongly typed model.
/// </summary>
internal interface IInlineQueryParameterBinder
{
    /// <summary>
    /// Binds inline query parameters from the context to a new instance of <typeparamref name="TParams"/>.
    /// </summary>
    /// <typeparam name="TParams">The type of the parameter model to bind to.</typeparam>
    /// <param name="context">The inline query execution context.</param>
    /// <returns>A populated instance of the parameter model.</returns>
    TParams Bind<TParams>(InlineQueryContext context) where TParams : class, new();
}
