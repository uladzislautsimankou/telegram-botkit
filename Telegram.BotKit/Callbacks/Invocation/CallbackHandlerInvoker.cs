using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Models;

namespace Telegram.BotKit.Callbacks.Invocation;

/// <summary>
/// Standard generic invoker that binds incoming parameters and routes execution to the target callback handler.
/// </summary>
internal sealed class CallbackHandlerInvoker<TParams>(
    ICallbackHandler<TParams> handler,
    ICallbackParameterBinder binder
    ) : ICallbackHandlerInvoker where TParams : class, new()
{
    public string Key => handler.Key;

    public async Task InvokeAsync(CallbackContext context, CancellationToken cancellationToken = default)
    {
        var parameters = binder.Bind<TParams>(context);
        await handler.HandleAsync(parameters, context, cancellationToken);
    }
}
