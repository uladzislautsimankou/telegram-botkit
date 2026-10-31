using Telegram.BotKit.Callbacks.Models;

namespace Telegram.BotKit.Callbacks.Invocation;

/// <summary>
/// Represents a static delegate that binds incoming parameters and invokes a callback handler.
/// </summary>
/// <param name="handlerInstance">The handler instance to invoke.</param>
/// <param name="context">The context of the callback query.</param>
/// <param name="serviceProvider">The service provider used to resolve the parameter binder.</param>
/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
internal delegate Task CallbackInvokerDelegate(
    object handlerInstance,
    CallbackContext context,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken);

/// <summary>
/// Encapsulates compiled factory instantiation and execution logic for a registered callback handler.
/// </summary>
/// <param name="HandlerType">The concrete handler type.</param>
/// <param name="ParamsType">The parameter model type of the handler.</param>
/// <param name="Factory">Creates the handler instance from a service provider.</param>
/// <param name="Invoker">Binds the parameters and invokes the handler.</param>
internal sealed record CallbackRegistration(
    Type HandlerType,
    Type ParamsType,
    Func<IServiceProvider, object> Factory,
    CallbackInvokerDelegate Invoker);


