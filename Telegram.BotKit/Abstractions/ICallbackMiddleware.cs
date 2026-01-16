
namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Represents a middleware component that processes callback queries in the pipeline.
/// </summary>
public interface ICallbackMiddleware
{
    /// <summary>
    /// Executes the middleware logic.
    /// Implementation should invoke <paramref name="next"/> to pass control to the subsequent middleware in the pipeline.
    /// </summary>
    /// <param name="context">The context associated with the current callback query.</param>
    /// <param name="next">The delegate representing the next middleware in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the execution of the middleware.</returns>
    Task InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default);
}
