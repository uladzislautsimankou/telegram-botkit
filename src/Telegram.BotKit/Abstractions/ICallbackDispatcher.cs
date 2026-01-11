using Telegram.Bot.Types;

namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Defines a mechanism for dispatching callback queries (inline button clicks) to the middleware pipeline.
/// </summary>
public interface ICallbackDispatcher
{
    /// <summary>
    /// Processes the incoming callback query, executes the middleware chain, and invokes the matching handler.
    /// </summary>
    /// <param name="query">The callback query received from Telegram.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DispatchAsync(CallbackQuery query, CancellationToken cancellationToken = default);
}
