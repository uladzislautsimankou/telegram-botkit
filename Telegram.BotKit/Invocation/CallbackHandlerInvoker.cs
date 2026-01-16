using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;

namespace Telegram.BotKit.Invocation;

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
