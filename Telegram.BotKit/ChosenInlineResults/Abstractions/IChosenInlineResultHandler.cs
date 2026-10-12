using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Models;

namespace Telegram.BotKit.ChosenInlineResults.Abstractions;

/// <summary>
/// Defines a handler for processing incoming chosen inline results.
/// </summary>
public interface IChosenInlineResultHandler
{
    /// <summary>
    /// Determines whether this handler can process the chosen inline result.
    /// </summary>
    /// <param name="chosenResult">The chosen inline result to evaluate.</param>
    /// <returns>True if this handler can process the result; otherwise, false.</returns>
    bool CanHandle(ChosenInlineResult chosenResult);

    /// <summary>
    /// Handles the chosen inline result execution.
    /// </summary>
    /// <param name="context">The context containing the chosen inline result and metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken = default);
}