namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Represents a middleware component that processes inline queries in the pipeline.
/// </summary>
public interface IInlineQueryMiddleware
{
    /// <summary>
    /// Executes the middleware logic.
    /// Implementation should invoke <paramref name="next"/> to pass control to the subsequent middleware in the pipeline.
    /// </summary>
    /// <param name="context">The context associated with the current inline query.</param>
    /// <param name="next">The delegate representing the next middleware in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the execution of the middleware.</returns>
    Task InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default);
}
