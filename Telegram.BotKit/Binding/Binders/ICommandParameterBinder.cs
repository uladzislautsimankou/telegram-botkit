namespace Telegram.BotKit.Binding.Binders;

internal interface ICommandParameterBinder
{
    TParams Bind<TParams>(CommandContext context) where TParams : class, new();
}

