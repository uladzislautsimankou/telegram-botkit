using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.WidgetQueries.Models;

/// <summary>
/// Encapsulates runtime metadata about the matched widget query handler.
/// </summary>
/// <param name="handlerType">The concrete type of the matched handler.</param>
/// <param name="instanceFactory">A factory creating the handler instance; invoked at most once.</param>
public sealed class WidgetQueryMetadata(Type handlerType, Func<object> instanceFactory)
    : HandlerMetadata(handlerType, instanceFactory);
