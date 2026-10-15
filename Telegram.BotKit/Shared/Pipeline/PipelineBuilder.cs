using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Abstract base class for configuring and compiling an update processing pipeline.
/// </summary>
/// <typeparam name="TMiddleware">The base middleware interface type for the pipeline branch.</typeparam>
/// <typeparam name="TContext">The context type processed by the pipeline.</typeparam>
public abstract class PipelineBuilder<TMiddleware, TContext>(IServiceCollection services)
    where TMiddleware : class
{
    public IServiceCollection Services { get; } = services;

    internal List<PipelineStep> Steps { get; } = [];

    /// <summary>
    /// Registers a custom middleware component in the pipeline.
    /// </summary>
    /// <typeparam name="TImplementation">The concrete type of the middleware to register.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public PipelineBuilder<TMiddleware, TContext> UseMiddleware<TImplementation>()
        where TImplementation : class, TMiddleware
    {
        Services.TryAddTransient<TImplementation>();
        Steps.Add(new PipelineStep(PipelineStepType.UserMiddleware, typeof(TImplementation)));
        return this;
    }

    /// <summary>
    /// Compiles all configured pipeline steps into a single reusable execution delegate.
    /// </summary>
    /// <param name="invoker">The delegate responsible for invoking a concrete middleware instance.</param>
    /// <returns>A compiled pipeline branch delegate.</returns>
    internal PipelineDelegate<TContext> Build(
        Func<TMiddleware, TContext, NextDelegate, CancellationToken, ValueTask> invoker)
    {
        // terminal step representing completion of the pipeline branch
        PipelineDelegate<TContext> pipeline = static (_, _, _) => ValueTask.CompletedTask;

        // fold steps in reverse order to form the invocation chain
        for (var i = Steps.Count - 1; i >= 0; i--)
        {
            var step = Steps[i];
            var next = pipeline;

            pipeline = (context, sp, ct) =>
            {
                // resolve middleware lazily only when execution reaches this step
                var middleware = (TMiddleware)sp.GetRequiredService(step.MiddlewareType);
                return invoker(middleware, context, () => next(context, sp, ct), ct);
            };
        }

        return pipeline;
    }
}
