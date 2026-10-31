using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Commands.Models;

/// <summary>
/// Encapsulates runtime metadata about the matched command handler. The handler is created lazily.
/// </summary>
/// <param name="handlerType">The concrete type of the matched handler.</param>
/// <param name="instanceFactory">A factory creating the handler instance; invoked at most once.</param>
public sealed class CommandMetadata(Type handlerType, Func<object> instanceFactory)
    : HandlerMetadata(handlerType, instanceFactory);
