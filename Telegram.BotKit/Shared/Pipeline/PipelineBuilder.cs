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

/// <summary>
/// Abstract base class for configuring and compiling an update processing pipeline.
/// </summary>
/// <typeparam name="TSelf">The concrete builder type, returned from fluent methods to keep the chain strongly typed.</typeparam>
/// <typeparam name="TMiddleware">The base middleware interface type for the pipeline branch.</typeparam>
/// <typeparam name="TContext">The context type processed by the pipeline.</typeparam>
/// <param name="services">The service collection the pipeline components are registered in.</param>
public abstract class PipelineBuilder<TSelf, TMiddleware, TContext>(IServiceCollection services)
    where TSelf : PipelineBuilder<TSelf, TMiddleware, TContext>
    where TMiddleware : class, IPipelineMiddleware<TContext>
{
    /// <summary>
    /// Gets the service collection the pipeline components are registered in.
    /// </summary>
    public IServiceCollection Services { get; } = services;

    /// <summary>
    /// Gets the configured steps of the pipeline in execution order.
    /// </summary>
    internal List<PipelineStep> Steps { get; } = [];

    /// <summary>
    /// Gets the type of the middleware that routes an update to its handler.
    /// </summary>
    internal abstract Type RoutingMiddlewareType { get; }

    /// <summary>
    /// Gets the type of the final middleware that runs the matched handler.
    /// </summary>
    internal abstract Type ExecutionMiddlewareType { get; }

    /// <summary>
    /// Explicitly defines the routing position in the pipeline.
    /// Middleware registered before this call runs before routing, middleware registered after it runs after routing.
    /// If routing is already configured, subsequent calls are ignored.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public TSelf UseRouting()
    {
        if (Steps.All(s => s.Type != PipelineStepType.Routing))
        {
            Steps.Add(new PipelineStep(PipelineStepType.Routing, RoutingMiddlewareType));
        }

        return (TSelf)this;
    }

    /// <summary>
    /// Registers a custom middleware component in the pipeline.
    /// Position relative to <c>UseRouting()</c> defines whether it runs before or after routing.
    /// </summary>
    /// <typeparam name="TImplementation">The concrete type of the middleware to register.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public TSelf UseMiddleware<TImplementation>()
        where TImplementation : class, TMiddleware
    {
        Services.TryAddTransient<TImplementation>();
        Steps.Add(new PipelineStep(PipelineStepType.UserMiddleware, typeof(TImplementation)));
        return (TSelf)this;
    }

    /// <summary>
    /// Adds routing (before all user middleware, unless <see cref="UseRouting"/> was called) and the execution step (last).
    /// </summary>
    internal void EnsureRoutingAndExecution()
    {
        if (Steps.All(s => s.Type != PipelineStepType.Routing))
        {
            Steps.Insert(0, new PipelineStep(PipelineStepType.Routing, RoutingMiddlewareType));
        }

        if (Steps.All(s => s.Type != PipelineStepType.Execution))
        {
            Steps.Add(new PipelineStep(PipelineStepType.Execution, ExecutionMiddlewareType));
        }
    }

    /// <summary>
    /// Completes the steps, compiles the pipeline and registers it together with its dispatcher.
    /// </summary>
    /// <typeparam name="TDispatcherService">The public dispatcher interface.</typeparam>
    /// <typeparam name="TDispatcher">The dispatcher implementation.</typeparam>
    internal void RegisterPipeline<TDispatcherService, TDispatcher>()
        where TDispatcherService : class
        where TDispatcher : class, TDispatcherService
    {
        EnsureRoutingAndExecution();

        Services.AddSingleton(Build());
        Services.AddSingleton<TDispatcherService, TDispatcher>();
    }

    /// <summary>
    /// Compiles all configured pipeline steps into a single reusable execution delegate.
    /// </summary>
    /// <returns>The compiled pipeline.</returns>
    internal PipelineDelegate<TContext> Build()
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
                return middleware.InvokeAsync(context, () => next(context, sp, ct), ct);
            };
        }

        return pipeline;
    }
}
