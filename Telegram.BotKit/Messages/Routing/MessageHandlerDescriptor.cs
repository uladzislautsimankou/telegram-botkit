using Telegram.BotKit.Messages.Models;

namespace Telegram.BotKit.Messages.Routing;

internal readonly record struct MessageHandlerDescriptor(
    Type HandlerType,
    Func<MessageContext, bool> CanHandle,
    Func<IServiceProvider, object> Factory);
