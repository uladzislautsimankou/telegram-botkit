using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.BotKit.Messages;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the message execution pipeline and register message middleware and handlers.
/// </summary>
public sealed class MessagePipelineBuilder(IServiceCollection services)
    : PipelineBuilder<IMessageMiddleware, MessageContext>(services)
{
    /// <summary>
    /// Explicitly defines the routing position in the pipeline.
    /// If routing is already configured, subsequent calls are ignored.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public MessagePipelineBuilder UseRouting()
    {
        // prevent duplicate routing steps in the pipeline
        if (Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            return this;
        }

        Steps.Add(new PipelineStep(PipelineStepType.Routing, typeof(MessageRoutingMiddleware)));
        return this;
    }

    /// <summary>
    /// Manually registers a specific message handler and registers it in the registry.
    /// </summary>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="IMessageHandler"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public MessagePipelineBuilder AddHandler<THandler>() where THandler : class, IMessageHandler
    {
        var registry = GetOrCreateRegistry();
        MessageRegistrationHelper.RegisterHandler<THandler>(Services, registry);
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

/// <summary>
/// Extension methods for configuring standard message handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class MessageServiceCollectionExtensions
{
    /// <summary>
    /// Configures the message pipeline and automatically registers all message handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    public static IServiceCollection AddMessages(
        this IServiceCollection services,
        Action<MessagePipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new MessageHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new MessagePipelineBuilder(services);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var assembly in targetAssemblies)
        {
            RegisterHandlersFromAssembly(services, assembly, registry);
        }

        services.AddTransient<MessageRoutingMiddleware>();
        services.AddTransient<MessageExecutionMiddleware>();

        configure?.Invoke(builder);

        registry.Build();

        if (builder.Steps.Count is 0)
        {
            builder.UseRouting();
        }
        else if (!builder.Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            builder.Steps.Insert(0, new PipelineStep(PipelineStepType.Routing, typeof(MessageRoutingMiddleware)));
        }

        builder.Steps.Add(new PipelineStep(PipelineStepType.Execution, typeof(MessageExecutionMiddleware)));

        var compiledPipeline = builder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        services.AddSingleton(compiledPipeline);
        services.AddSingleton<IMessageDispatcher, MessageDispatcher>();

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly, MessageHandlerRegistry registry)
    {
        var handlerType = typeof(IMessageHandler);

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
                MessageRegistrationHelper.RegisterHandler(services, registry, type);
            }
        }
    }
}

internal static class MessageRegistrationHelper
{
    public static void RegisterHandler<THandler>(IServiceCollection services, MessageHandlerRegistry registry)
        where THandler : class, IMessageHandler
    {
        services.TryAddTransient<THandler>();

        var requiredState = THandler.RequiredState?.Trim();
        if (string.IsNullOrWhiteSpace(requiredState))
        {
            requiredState = null;
        }

        registry.Register(new MessageHandlerDescriptor(
            typeof(THandler),
            THandler.CanHandle,
            static sp => sp.GetRequiredService<THandler>()
        ), requiredState);
    }

    public static void RegisterHandler(IServiceCollection services, MessageHandlerRegistry registry, Type handlerType)
    {
        services.TryAddTransient(handlerType);

        var stateProp = handlerType.GetProperty(nameof(IMessageHandler.RequiredState), BindingFlags.Public | BindingFlags.Static);
        var rawState = (string?)stateProp?.GetValue(null);
        var requiredState = string.IsNullOrWhiteSpace(rawState) ? null : rawState.Trim();

        var canHandleMethod = handlerType.GetMethod(
            nameof(IMessageHandler.CanHandle),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(MessageContext)])
            ?? throw new InvalidOperationException($"Handler '{handlerType.FullName}' must implement static bool CanHandle(MessageContext context).");

        var canHandle = (Func<MessageContext, bool>)Delegate.CreateDelegate(typeof(Func<MessageContext, bool>), canHandleMethod);

        registry.Register(new MessageHandlerDescriptor(
            handlerType,
            canHandle,
            sp => sp.GetRequiredService(handlerType)
        ), requiredState);
    }
}
