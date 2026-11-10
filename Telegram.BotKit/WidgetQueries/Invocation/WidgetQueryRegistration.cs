using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.WidgetQueries.Invocation;

/// <summary>
/// Represents a static delegate that binds incoming parameters and invokes a widget query handler.
/// </summary>
/// <param name="handlerInstance">The handler instance to invoke.</param>
/// <param name="context">The context of the widget query.</param>
/// <param name="serviceProvider">The service provider used to resolve the parameter binder.</param>
/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
internal delegate Task WidgetQueryInvokerDelegate(
    object handlerInstance,
    WidgetQueryContext context,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken);

/// <summary>
/// Encapsulates compiled factory instantiation and execution logic for a registered widget query handler.
/// </summary>
/// <param name="HandlerType">The concrete handler type.</param>
/// <param name="ParamsType">The parameter model type of the handler.</param>
/// <param name="Factory">Creates the handler instance from a service provider.</param>
/// <param name="Invoker">Binds the parameters and invokes the handler.</param>
internal sealed record WidgetQueryRegistration(
    Type HandlerType,
    Type ParamsType,
    Func<IServiceProvider, object> Factory,
    WidgetQueryInvokerDelegate Invoker);
