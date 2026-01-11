namespace Telegram.BotKit.Invocation;

internal interface ICallbackHandlerInvoker
{
    string Key { get; }

    Task InvokeAsync(CallbackContext context, CancellationToken cancellationToken = default);
}
