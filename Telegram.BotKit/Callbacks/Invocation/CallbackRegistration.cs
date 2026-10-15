using Telegram.BotKit.Callbacks.Models;

namespace Telegram.BotKit.Callbacks.Invocation;

/// <summary>
/// Represents a static delegate that binds incoming parameters and invokes a callback handler.
/// </summary>
internal delegate Task CallbackInvokerDelegate(
    object handlerInstance,
    CallbackContext context,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken);

/// <summary>
/// Encapsulates compiled factory instantiation and execution logic for a registered callback handler.
/// </summary>
internal sealed record CallbackRegistration(
    Type HandlerType,
    Func<IServiceProvider, object> Factory,
    CallbackInvokerDelegate Invoker);
