using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.ChosenInlineResults.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the chosen inline result execution pipeline and register middleware and handlers.
/// </summary>
public sealed class ChosenInlineResultPipelineBuilder(IServiceCollection services)
    : PipelineBuilder<IChosenInlineResultMiddleware, ChosenInlineResultContext>(services)
{
    /// <summary>
    /// Explicitly defines the routing position in the pipeline.
    /// If routing is already configured, subsequent calls are ignored.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public ChosenInlineResultPipelineBuilder UseRouting()
    {
        // prevent duplicate routing steps in the pipeline
        if (Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            return this;
        }

        Steps.Add(new PipelineStep(PipelineStepType.Routing, typeof(ChosenInlineResultRoutingMiddleware)));
        return this;
    }

    /// <summary>
    /// Manually registers a specific chosen inline result handler.
    /// </summary>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="IChosenInlineResultHandler"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public ChosenInlineResultPipelineBuilder AddHandler<THandler>() where THandler : class, IChosenInlineResultHandler
    {
        var registry = GetOrCreateRegistry();
        ChosenInlineResultRegistrationHelper.RegisterHandler<THandler>(Services, registry);
        return this;
    }

    private ChosenInlineResultHandlerRegistry GetOrCreateRegistry()
    {
        var registryDescriptor = Services.FirstOrDefault(d => d.ServiceType == typeof(ChosenInlineResultHandlerRegistry));
        if (registryDescriptor?.ImplementationInstance is ChosenInlineResultHandlerRegistry r)
        {
            return r;
        }

        var registry = new ChosenInlineResultHandlerRegistry();
        Services.AddSingleton(registry);
        return registry;
    }
}

/// <summary>
/// Extension methods for configuring chosen inline result handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class ChosenInlineResultServiceCollectionExtensions
{
    /// <summary>
    /// Configures the chosen inline result pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    public static IServiceCollection AddChosenInlineResults(
        this IServiceCollection services,
        Action<ChosenInlineResultPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new ChosenInlineResultHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new ChosenInlineResultPipelineBuilder(services);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var assembly in targetAssemblies)
        {
            RegisterHandlersFromAssembly(services, assembly, registry);
        }

        services.AddTransient<ChosenInlineResultRoutingMiddleware>();
        services.AddTransient<ChosenInlineResultExecutionMiddleware>();

        configure?.Invoke(builder);

        registry.Build();

        if (builder.Steps.Count is 0)
        {
            builder.UseRouting();
        }
        else if (!builder.Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            builder.Steps.Insert(0, new PipelineStep(PipelineStepType.Routing, typeof(ChosenInlineResultRoutingMiddleware)));
        }

        builder.Steps.Add(new PipelineStep(PipelineStepType.Execution, typeof(ChosenInlineResultExecutionMiddleware)));

        var compiledPipeline = builder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        services.AddSingleton(compiledPipeline);
        services.AddSingleton<IChosenInlineResultDispatcher, ChosenInlineResultDispatcher>();

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly, ChosenInlineResultHandlerRegistry registry)
    {
        var handlerType = typeof(IChosenInlineResultHandler);

        Type[] allTypes;
        try
        {
            allTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            allTypes = ex.Types.Where(t => t is not null).ToArray()!;
        }

        for (var i = 0; i < allTypes.Length; i++)
        {
            var type = allTypes[i];
            if (!type.IsAbstract && !type.IsInterface && handlerType.IsAssignableFrom(type))
            {
                ChosenInlineResultRegistrationHelper.RegisterHandler(services, registry, type);
            }
        }
    }
}

internal static class ChosenInlineResultRegistrationHelper
{
    public static void RegisterHandler<THandler>(IServiceCollection services, ChosenInlineResultHandlerRegistry registry)
        where THandler : class, IChosenInlineResultHandler
    {
        services.TryAddTransient<THandler>();

        registry.Register(new ChosenInlineResultHandlerDescriptor(
            typeof(THandler),
            THandler.CanHandle,
            static sp => sp.GetRequiredService<THandler>()
        ));
    }

    public static void RegisterHandler(IServiceCollection services, ChosenInlineResultHandlerRegistry registry, Type handlerType)
    {
        services.TryAddTransient(handlerType);

        var canHandleMethod = handlerType.GetMethod(
            nameof(IChosenInlineResultHandler.CanHandle),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(ChosenInlineResult)])
            ?? throw new InvalidOperationException($"Handler '{handlerType.FullName}' must implement static bool CanHandle(ChosenInlineResult chosenResult).");

        var canHandle = (Func<ChosenInlineResult, bool>)Delegate.CreateDelegate(typeof(Func<ChosenInlineResult, bool>), canHandleMethod);

        registry.Register(new ChosenInlineResultHandlerDescriptor(
            handlerType,
            canHandle,
            sp => sp.GetRequiredService(handlerType)
        ));
    }
}