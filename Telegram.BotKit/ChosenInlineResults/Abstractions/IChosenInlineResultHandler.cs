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
    /// Evaluated prior to resolving the handler from the service provider.
    /// </summary>
    /// <param name="chosenResult">The chosen inline result to evaluate.</param>
    /// <returns>True if this handler can process the result; otherwise, false.</returns>
    static abstract bool CanHandle(ChosenInlineResult chosenResult);

    /// <summary>
    /// Gets the priority of this handler among the handlers evaluated for the same update: lower values go first, the default is 0.
    /// Handlers with equal priority keep registration order: handlers found by assembly scanning are registered
    /// ordered by full type name, handlers added with AddHandler follow in call order.
    /// Message and chosen inline result updates are processed by the first handler whose CanHandle returns true;
    /// for inline queries all matching handlers run and Order defines the order of their results.
    /// </summary>
    static virtual int Order => 0;

    /// <summary>
    /// Handles the chosen inline result execution.
    /// </summary>
    /// <param name="context">The context containing the chosen inline result and metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken = default);
}
