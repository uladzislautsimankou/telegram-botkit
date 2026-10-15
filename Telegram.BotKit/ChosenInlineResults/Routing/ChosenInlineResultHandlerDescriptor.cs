using Telegram.Bot.Types;

namespace Telegram.BotKit.ChosenInlineResults.Routing;

internal readonly record struct ChosenInlineResultHandlerDescriptor(
    Type HandlerType,
    Func<ChosenInlineResult, bool> CanHandle,
    Func<IServiceProvider, object> Factory);
