using Telegram.BotKit.Callbacks.Models;

namespace Telegram.BotKit.Callbacks.Invocation;

/// <summary>
/// Defines an invoker contract for executing a matched callback handler.
/// </summary>
internal interface ICallbackHandlerInvoker
{
    /// <summary>
    /// Gets the registration key for the handler.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Invokes the underlying callback handler execution flow.
    /// </summary>
    Task InvokeAsync(CallbackContext context, CancellationToken cancellationToken = default);
}
