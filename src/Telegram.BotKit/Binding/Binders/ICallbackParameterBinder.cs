namespace Telegram.BotKit.Binding.Binders;

internal interface ICallbackParameterBinder
{
    TParams Bind<TParams>(CallbackContext context) where TParams : class, new();
}
