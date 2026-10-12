using Telegram.BotKit.Commands.Models;

namespace Telegram.BotKit.Commands.Invocation;

/// <summary>
/// Represents a static delegate that binds incoming parameters and invokes a command handler.
/// </summary>
internal delegate Task CommandInvokerDelegate(
    object handlerInstance,
    CommandContext context,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken);

/// <summary>
/// Encapsulates compiled factory instantiation and execution logic for a registered command handler.
/// </summary>
internal sealed record CommandRegistration(
    Type HandlerType,
    Func<IServiceProvider, object> Factory,
    CommandInvokerDelegate Invoker);