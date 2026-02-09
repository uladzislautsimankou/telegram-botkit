namespace Telegram.BotKit.Binding.Binders;

internal interface IInlineQueryParameterBinder
{
    TParams Bind<TParams>(InlineQueryContext context) where TParams : class, new();
}
