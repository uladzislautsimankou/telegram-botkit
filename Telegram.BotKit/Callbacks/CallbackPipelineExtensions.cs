using System.Reflection;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Callbacks;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class CallbackPipelineBuilder(IServiceCollection services) : PipelineBuilder<ICallbackMiddleware>(services)
{
    /// <summary>
    /// Explicitly defines the routing position in the pipeline.
    /// </summary>
    public CallbackPipelineBuilder UseRouting()
    {
        Steps.Add(new PipelineStep(PipelineStepType.Routing, typeof(CallbackRoutingMiddleware)));
        return this;
    }

    /// <summary>
    /// Manually registers a specific callback handler and registers its corresponding invoker.
    /// </summary>
    public CallbackPipelineBuilder AddHandler<THandler>() where THandler : class
    {
        var handlerType = typeof(THandler);
        var registry = GetOrCreateRegistry();

        var callbackInterfaces = handlerType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICallbackHandler<>))
            .ToList();

        if (callbackInterfaces.Count is 0)
        {
            throw new InvalidOperationException($"Type '{handlerType.Name}' does not implement ICallbackHandler<TParams>.");
        }

        foreach (var callbackInterface in callbackInterfaces)
        {
            // register the handler itself under its interface ICallbackHandler<TParams>
            Services.AddTransient(callbackInterface, handlerType);

            // extract TParams
            var paramsType = callbackInterface.GetGenericArguments();

            // dynamically create the CallbackHandlerInvoker<TParams> type
            var concreteInvokerType = typeof(CallbackHandlerInvoker<>).MakeGenericType(paramsType);

            // register the type in DI and add it to the registry
            Services.AddTransient(concreteInvokerType);
            registry.RegisterInvokerType(concreteInvokerType);
        }

        return this;
    }

    private CallbackHandlerRegistry GetOrCreateRegistry()
    {
        var registryDescriptor = Services.FirstOrDefault(d => d.ServiceType == typeof(CallbackHandlerRegistry));
        if (registryDescriptor?.ImplementationInstance is CallbackHandlerRegistry r)
        {
            return r;
        }

        var registry = new CallbackHandlerRegistry();
        Services.AddSingleton(registry);
        return registry;
    }
}

public static class CallbackServiceCollectionExtensions
{
    /// <summary>
    /// Configures the callback pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    public static IServiceCollection AddCallbacks(
        this IServiceCollection services,
        Action<CallbackPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new CallbackHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new CallbackPipelineBuilder(services);

        // automatically scan assemblies, register handlers, and generate invokers for them
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;
        foreach (var assembly in targetAssemblies)
        {
            RegisterHandlersAndInvokers(services, assembly, registry);
        }

        // register required system services
        services.AddTransient<CallbackRoutingMiddleware>();
        services.AddTransient<CallbackExecutionMiddleware>();

        // configure user pipeline
        configure?.Invoke(builder);

        // default settings if the pipeline is empty
        if (builder.Steps.Count is 0)
        {
            builder.UseRouting();
        }
        else if (!builder.Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            builder.Steps.Insert(0, new PipelineStep(PipelineStepType.Routing, typeof(CallbackRoutingMiddleware)));
        }

        // executor is always last
        builder.Steps.Add(new PipelineStep(PipelineStepType.Execution, typeof(CallbackExecutionMiddleware)));

        services.AddSingleton(builder);
        services.AddSingleton<ICallbackDispatcher, CallbackDispatcher>();
        services.AddTransient<ICallbackParameterBinder, CallbackParameterBinder>();

        return services;
    }

    private static void RegisterHandlersAndInvokers(IServiceCollection services, Assembly assembly, CallbackHandlerRegistry registry)
    {
        var genericInterfaceType = typeof(ICallbackHandler<>);
        var genericInvokerType = typeof(CallbackHandlerInvoker<>);

        var handlerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType)
                .Select(i => new { Interface = i, Implementation = t }))
            .ToList();

        foreach (var typeInfo in handlerTypes)
        {
            // register the handler under its closed interface (e.g., ICallbackHandler<StartParams>)
            services.AddTransient(typeInfo.Interface, typeInfo.Implementation);

            // extract TParams from ICallbackHandler<TParams>
            var paramsType = typeInfo.Interface.GetGenericArguments();

            // create a closed invoker type: CallbackHandlerInvoker<StartParams>
            var concreteInvokerType = genericInvokerType.MakeGenericType(paramsType);

            // register the invoker and add it to the registry
            services.AddTransient(concreteInvokerType);
            registry.RegisterInvokerType(concreteInvokerType);
        }

    }
}