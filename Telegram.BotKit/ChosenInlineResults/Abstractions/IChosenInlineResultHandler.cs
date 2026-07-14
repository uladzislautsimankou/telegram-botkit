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
    bool CanHandle(ChosenInlineResult chosenResult);

    /// <summary>
    /// Handles the chosen inline result execution.
    /// </summary>
    Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken = default);
}