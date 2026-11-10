using Telegram.BotKit.Shared.Binding;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.WidgetQueries.Binding;

/// <summary>
/// Defines a binder responsible for mapping the parameters of a widget query to a strongly typed model.
/// </summary>
internal interface IWidgetParameterBinder : IParameterModelValidator
{
    /// <summary>
    /// Binds widget query parameters from the context to a new instance of <typeparamref name="TParams"/>.
    /// </summary>
    /// <typeparam name="TParams">The type of the parameter model to bind to.</typeparam>
    /// <param name="context">The widget query execution context.</param>
    /// <returns>A populated instance of the parameter model.</returns>
    TParams Bind<TParams>(WidgetQueryContext context) where TParams : class, new();
}
