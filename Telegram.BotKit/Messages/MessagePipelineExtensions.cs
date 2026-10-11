using System.Reflection;
using Telegram.BotKit.Messages;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class MessagePipelineBuilder(IServiceCollection services)
    : PipelineBuilder<IMessageMiddleware>(services)
{
    /// <summary>
    /// Explicitly defines the routing position in the pipeline.
    /// </summary>
    public MessagePipelineBuilder UseRouting()
    {
        Steps.Add(new PipelineStep(PipelineStepType.Routing, typeof(MessageRoutingMiddleware)));
        return this;
    }

    /// <summary>
    /// Manually registers a specific message handler and registers it in the registry.
    /// </summary>
    public MessagePipelineBuilder AddHandler<THandler>() where THandler : class, IMessageHandler
    {
        var handlerType = typeof(THandler);

        Services.AddTransient<THandler>();
        Services.AddTransient<IMessageHandler, THandler>();

        var registry = GetOrCreateRegistry();

        var stateProp = handlerType.GetProperty("RequiredState", BindingFlags.Public | BindingFlags.Static);
        var rawState = (string?)stateProp?.GetValue(null);
        var requiredState = string.IsNullOrWhiteSpace(rawState) ? null : rawState.Trim();

        registry.Register(handlerType, requiredState);

        return this;
    }

    private MessageHandlerRegistry GetOrCreateRegistry()
    {
        var registryDescriptor = Services.FirstOrDefault(d => d.ServiceType == typeof(MessageHandlerRegistry));
        if (registryDescriptor?.ImplementationInstance is MessageHandlerRegistry r)
        {
            return r;
        }

        var registry = new MessageHandlerRegistry();
        Services.AddSingleton(registry);
        return registry;
    }
}

public static class MessageServiceCollectionExtensions
{
    /// <summary>
    /// Configures the message pipeline and automatically registers all message handlers from the specified assemblies.
    /// </summary>
    public static IServiceCollection AddMessages(
        this IServiceCollection services,
        Action<MessagePipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new MessageHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new MessagePipelineBuilder(services);

        // automatically scan assemblies, register handlers, and generate invokers for them
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;
        foreach (var assembly in targetAssemblies)
        {
            RegisterHandlersFromAssembly(services, assembly, registry);
        }

        // register required system services
        services.AddTransient<MessageRoutingMiddleware>();
        services.AddTransient<MessageExecutionMiddleware>();

        // configure user pipeline
        configure?.Invoke(builder);

        // default settings if the pipeline is empty
        if (builder.Steps.Count is 0)
        {
            builder.UseRouting();
        }
        else if (!builder.Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            builder.Steps.Insert(0, new PipelineStep(PipelineStepType.Routing, typeof(MessageRoutingMiddleware)));
        }

        // executor is always last
        builder.Steps.Add(new PipelineStep(PipelineStepType.Execution, typeof(MessageExecutionMiddleware)));

        services.AddSingleton(builder);
        services.AddSingleton<IMessageDispatcher, MessageDispatcher>();

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly, MessageHandlerRegistry registry)
    {
        var handlerType = typeof(IMessageHandler);

        // safely retrieve loadable types even if optional dependencies fail to resolve
        Type[] allTypes;
        try
        {
            allTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            allTypes = ex.Types.Where(t => t != null).ToArray()!;
        }

        var concreteHandlers = allTypes
            .Where(t => !t.IsAbstract && !t.IsInterface && handlerType.IsAssignableFrom(t));

        foreach (var type in concreteHandlers)
        {
            services.AddTransient(type);
            services.AddTransient(handlerType, type);

            var stateProp = type.GetProperty("RequiredState", BindingFlags.Public | BindingFlags.Static);
            var rawState = (string?)stateProp?.GetValue(null);
            var requiredState = string.IsNullOrWhiteSpace(rawState) ? null : rawState.Trim();

            registry.Register(type, requiredState);
        }
    }
}