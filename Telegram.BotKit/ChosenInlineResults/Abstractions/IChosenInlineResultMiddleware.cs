using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.ChosenInlineResults.Abstractions;

/// <summary>
/// Represents a middleware component that processes chosen inline results in the pipeline.
/// </summary>
public interface IChosenInlineResultMiddleware
{
    /// <summary>
    /// Executes the middleware logic.
    /// </summary>
    /// <param name="context">The context associated with the current chosen inline result.</param>
    /// <param name="next">The delegate representing the next middleware in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the execution of the middleware.</returns>
    ValueTask InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default);
}
