using Telegram.Bot.Types;

namespace Telegram.BotKit.ChosenInlineResults.Abstractions;

/// <summary>
/// Defines a mechanism for dispatching chosen inline result updates to the middleware pipeline.
/// </summary>
public interface IChosenInlineResultDispatcher
{
    /// <summary>
    /// Processes the incoming chosen inline result update, executes the middleware chain, and invokes the matching handler.
    /// </summary>
    /// <param name="chosenResult">The chosen inline result received from Telegram.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DispatchAsync(ChosenInlineResult chosenResult, CancellationToken cancellationToken = default);
}
