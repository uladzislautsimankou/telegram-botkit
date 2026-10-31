using Telegram.Bot.Types;

namespace Telegram.BotKit.InlineQueries.Routing;

/// <summary>
/// Describes a registered inline query handler.
/// </summary>
/// <param name="HandlerType">The concrete handler type.</param>
/// <param name="CanHandle">The static matching predicate of the handler.</param>
/// <param name="Factory">Creates the handler instance from a service provider.</param>
/// <param name="Order">The priority of the handler: lower values are evaluated first.</param>
internal readonly record struct InlineQueryHandlerDescriptor(
    Type HandlerType,
    Func<InlineQuery, bool> CanHandle,
    Func<IServiceProvider, object> Factory,
    int Order = 0);
