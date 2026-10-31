using Microsoft.Extensions.DependencyInjection;

namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Base class of dispatchers: runs a compiled pipeline in a dedicated dependency scope.
/// </summary>
/// <typeparam name="TContext">The context type processed by the pipeline.</typeparam>
/// <param name="serviceProvider">The root service provider used to create a dependency scope for each update.</param>
/// <param name="pipeline">The compiled middleware pipeline.</param>
internal abstract class DispatcherBase<TContext>(IServiceProvider serviceProvider, PipelineDelegate<TContext> pipeline)
{
    /// <summary>
    /// Creates a dependency scope and runs the pipeline for the context in it.
    /// </summary>
    /// <param name="context">The context of the incoming update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    protected async Task RunAsync(TContext context, CancellationToken cancellationToken)
    {
        // a dedicated async dependency scope for the incoming update
        await using var scope = serviceProvider.CreateAsyncScope();

        // execute the precompiled middleware delegate chain
        await pipeline(context, scope.ServiceProvider, cancellationToken);
    }
}
