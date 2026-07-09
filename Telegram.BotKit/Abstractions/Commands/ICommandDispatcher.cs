using Telegram.Bot.Types;

namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Defines a mechanism for dispatching command messages (messages starting with '/') to the middleware pipeline.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Processes the incoming command message, executes the middleware chain, and invokes the matching handler.
    /// </summary>
    /// <param name="message">The message received from Telegram containing the command.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DispatchAsync(Message message, CancellationToken cancellationToken = default);
}
