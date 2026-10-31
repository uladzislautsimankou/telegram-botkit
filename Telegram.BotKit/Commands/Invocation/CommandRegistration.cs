using Telegram.BotKit.Commands.Models;

namespace Telegram.BotKit.Commands.Invocation;

/// <summary>
/// Represents a static delegate that binds incoming parameters and invokes a command handler.
/// </summary>
/// <param name="handlerInstance">The handler instance to invoke.</param>
/// <param name="context">The context of the command message.</param>
/// <param name="serviceProvider">The service provider used to resolve the parameter binder.</param>
/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
internal delegate Task CommandInvokerDelegate(
    object handlerInstance,
    CommandContext context,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken);

/// <summary>
/// Encapsulates compiled factory instantiation and execution logic for a registered command handler.
/// </summary>
/// <param name="HandlerType">The concrete handler type.</param>
/// <param name="ParamsType">The parameter model type of the handler.</param>
/// <param name="Factory">Creates the handler instance from a service provider.</param>
/// <param name="Invoker">Binds the arguments and invokes the handler.</param>
internal sealed record CommandRegistration(
    Type HandlerType,
    Type ParamsType,
    Func<IServiceProvider, object> Factory,
    CommandInvokerDelegate Invoker);


