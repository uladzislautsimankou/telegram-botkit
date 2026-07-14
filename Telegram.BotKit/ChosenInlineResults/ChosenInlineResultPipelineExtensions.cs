using System.Reflection;
using Telegram.BotKit.ChosenInlineResults;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class ChosenInlineResultPipelineBuilder(IServiceCollection services)
    : PipelineBuilder<IChosenInlineResultMiddleware>(services)
{
    /// <summary>
    /// Explicitly defines the routing position in the pipeline.
    /// </summary>
    public ChosenInlineResultPipelineBuilder UseRouting()
    {
        Steps.Add(new PipelineStep(PipelineStepType.Routing, typeof(ChosenInlineResultRoutingMiddleware)));
        return this;
    }

    /// <summary>
    /// Manually registers a specific chosen inline result handler.
    /// </summary>
    public ChosenInlineResultPipelineBuilder AddHandler<THandler>() where THandler : class, IChosenInlineResultHandler
    {
        Services.AddTransient<THandler>();
        Services.AddTransient<IChosenInlineResultHandler, THandler>();
        return this;
    }
}

public static class ChosenInlineResultServiceCollectionExtensions
{
    /// <summary>
    /// Configures the chosen inline result pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    public static IServiceCollection AddChosenInlineResults(
        this IServiceCollection services,
        Action<ChosenInlineResultPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var builder = new ChosenInlineResultPipelineBuilder(services);

        // automatically scan assemblies, register handlers, and generate invokers for them
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;
        foreach (var assembly in targetAssemblies)
        {
            RegisterHandlersFromAssembly(services, assembly);
        }

        // register required system services
        services.AddTransient<ChosenInlineResultRoutingMiddleware>();
        services.AddTransient<ChosenInlineResultExecutionMiddleware>();

        // configure user pipeline
        configure?.Invoke(builder);

        // default settings if the pipeline is empty
        if (builder.Steps.Count is 0)
        {
            builder.UseRouting();
        }
        else if (!builder.Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            builder.Steps.Insert(0, new PipelineStep(PipelineStepType.Routing, typeof(ChosenInlineResultRoutingMiddleware)));
        }

        // executor is always last
        builder.Steps.Add(new PipelineStep(PipelineStepType.Execution, typeof(ChosenInlineResultExecutionMiddleware)));

        services.AddSingleton(builder);
        services.AddSingleton<IChosenInlineResultDispatcher, ChosenInlineResultDispatcher>();

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var handlerType = typeof(IChosenInlineResultHandler);

        var concreteHandlers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && handlerType.IsAssignableFrom(t));

        foreach (var type in concreteHandlers)
        {
            services.AddTransient(type);
            services.AddTransient(handlerType, type);
        }
    }
}