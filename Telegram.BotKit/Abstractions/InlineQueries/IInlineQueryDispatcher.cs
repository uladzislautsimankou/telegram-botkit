using Telegram.Bot.Types;

namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Defines a mechanism for dispatching inline queries (inline mode searches) to the middleware pipeline.
/// </summary>
public interface IInlineQueryDispatcher
{
    /// <summary>
    /// Processes the incoming inline query, executes the middleware chain, and invokes the matching handler.
    /// </summary>
    /// <param name="query">The inline query received from Telegram.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DispatchAsync(InlineQuery query, CancellationToken cancellationToken = default);
}
