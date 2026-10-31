namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Represents a middleware component of an update processing pipeline.
/// </summary>
/// <typeparam name="TContext">The context type processed by the pipeline.</typeparam>
public interface IPipelineMiddleware<TContext>
{
    /// <summary>
    /// Executes the middleware logic.
    /// Implementation should invoke <paramref name="next"/> to pass control to the subsequent middleware;
    /// not invoking it stops the pipeline for the current update.
    /// </summary>
    /// <param name="context">The context associated with the current update.</param>
    /// <param name="next">The delegate representing the next middleware in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    ValueTask InvokeAsync(TContext context, NextDelegate next, CancellationToken cancellationToken = default);
}
