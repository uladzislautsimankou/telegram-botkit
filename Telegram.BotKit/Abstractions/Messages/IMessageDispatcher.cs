using Telegram.Bot.Types;

namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Defines a mechanism for dispatching regular message updates to the middleware pipeline.
/// </summary>
public interface IMessageDispatcher
{
    /// <summary>
    /// Processes the incoming message update, executes the middleware chain, and invokes the matching handler.
    /// </summary>
    /// <param name="message">The message received from Telegram.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DispatchAsync(Message message, CancellationToken cancellationToken = default);
}