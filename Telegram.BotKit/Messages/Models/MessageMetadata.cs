using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Messages.Models;

/// <summary>
/// Encapsulates runtime metadata about the matched message handler.
/// </summary>
/// <param name="handlerType">The concrete type of the matched handler.</param>
/// <param name="instanceFactory">A factory creating the handler instance; invoked at most once.</param>
public sealed class MessageMetadata(Type handlerType, Func<object> instanceFactory)
    : HandlerMetadata(handlerType, instanceFactory);
