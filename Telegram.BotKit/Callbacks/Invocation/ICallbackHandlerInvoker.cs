using Telegram.BotKit.Callbacks.Models;

namespace Telegram.BotKit.Callbacks.Invocation;

/// <summary>
/// Defines an invoker contract for executing a matched callback handler.
/// </summary>
internal interface ICallbackHandlerInvoker
{
    /// <summary>
    /// Invokes the underlying callback handler execution flow.
    /// </summary>
    Task InvokeAsync(CallbackContext context, CancellationToken cancellationToken = default);
}
