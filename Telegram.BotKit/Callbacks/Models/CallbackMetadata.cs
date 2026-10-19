using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Callbacks.Models;

/// <summary>
/// Encapsulates runtime metadata about the matched callback handler.
/// </summary>
/// <param name="handlerType">The concrete type of the matched handler.</param>
/// <param name="instanceFactory">A factory creating the handler instance; invoked at most once.</param>
public sealed class CallbackMetadata(Type handlerType, Func<object> instanceFactory)
    : HandlerMetadata(handlerType, instanceFactory);
