using Microsoft.Extensions.DependencyInjection;

namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Abstract base class for building any typed middleware pipeline.
/// </summary>
/// <typeparam name="TMiddleware">The type of the middleware interface inside the current update branch.</typeparam>
public abstract class PipelineBuilder<TMiddleware>(IServiceCollection services) where TMiddleware : class
{
    public IServiceCollection Services { get; } = services;

    // keep properties internal so the end user only sees the public fluent api (.usemiddleware)
    internal List<PipelineStep> Steps { get; } = [];

    /// <summary>
    /// Registers a user-defined middleware in the pipeline.
    /// </summary>
    public PipelineBuilder<TMiddleware> UseMiddleware<TImplementation>()
        where TImplementation : class, TMiddleware
    {
        Services.AddTransient<TImplementation>();
        Steps.Add(new PipelineStep(PipelineStepType.UserMiddleware, typeof(TImplementation)));
        return this;
    }

    /// <summary>
    /// Helper method for adding system steps such as routing or execution by concrete builders.
    /// </summary>
    protected void AddStep(PipelineStepType type, Type middlewareType) => Steps.Add(new PipelineStep(type, middlewareType));
}