using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.ChosenInlineResults.Models;

/// <summary>
/// Encapsulates runtime metadata about the matched chosen inline result handler.
/// </summary>
/// <param name="handlerType">The concrete type of the matched handler.</param>
/// <param name="instanceFactory">A factory creating the handler instance; invoked at most once.</param>
public sealed class ChosenInlineResultMetadata(Type handlerType, Func<object> instanceFactory)
    : HandlerMetadata(handlerType, instanceFactory);
