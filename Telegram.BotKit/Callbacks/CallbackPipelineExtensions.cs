using System.Reflection;
using Telegram.BotKit.Callbacks;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the callback execution pipeline and register callback middleware and handlers.
/// </summary>
public sealed class CallbackPipelineBuilder(IServiceCollection services)
    : PipelineBuilder<ICallbackMiddleware, CallbackContext>(services)
{
    /// <summary>
    /// Explicitly defines the routing position in the pipeline.
    /// If routing is already configured, subsequent calls are ignored.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public CallbackPipelineBuilder UseRouting()
    {
        // prevent duplicate routing steps in the pipeline
        if (Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            return this;
        }

        Steps.Add(new PipelineStep(PipelineStepType.Routing, typeof(CallbackRoutingMiddleware)));
        return this;
    }

    /// <summary>
    /// Manually registers a specific callback handler and precompiles its factory and invoker.
    /// </summary>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="ICallbackHandler{TParams}"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public CallbackPipelineBuilder AddHandler<THandler>() where THandler : class
    {
        var registry = GetOrCreateRegistry();
        CallbackRegistrationHelper.RegisterHandler(Services, registry, typeof(THandler));
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

/// <summary>
/// Extension methods for configuring callback query handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class CallbackServiceCollectionExtensions
{
    /// <summary>
    /// Configures the callback pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    public static IServiceCollection AddCallbacks(
        this IServiceCollection services,
        Action<CallbackPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new CallbackHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new CallbackPipelineBuilder(services);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var assembly in targetAssemblies)
        {
            RegisterHandlersFromAssembly(services, assembly, registry);
        }

        services.AddTransient<CallbackRoutingMiddleware>();
        services.AddTransient<CallbackExecutionMiddleware>();

        configure?.Invoke(builder);

        if (builder.Steps.Count is 0)
        {
            builder.UseRouting();
        }
        else if (!builder.Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            builder.Steps.Insert(0, new PipelineStep(PipelineStepType.Routing, typeof(CallbackRoutingMiddleware)));
        }

        builder.Steps.Add(new PipelineStep(PipelineStepType.Execution, typeof(CallbackExecutionMiddleware)));

        registry.Build();

        var compiledPipeline = builder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        services.AddSingleton(compiledPipeline);
        services.AddSingleton<ICallbackDispatcher, CallbackDispatcher>();
        services.AddSingleton<ICallbackParameterBinder, CallbackParameterBinder>();

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly, CallbackHandlerRegistry registry)
    {
        var genericInterfaceType = typeof(ICallbackHandler<>);

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
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            var implementsInterface = type.GetInterfaces()
                .Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericInterfaceType);

            if (implementsInterface)
            {
                CallbackRegistrationHelper.RegisterHandler(services, registry, type);
            }
        }
    }
}

internal static class CallbackRegistrationHelper
{
    private static readonly MethodInfo CreateRegistrationMethod = typeof(CallbackRegistrationHelper)
        .GetMethod(nameof(CreateRegistration), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static void RegisterHandler(IServiceCollection services, CallbackHandlerRegistry registry, Type handlerType)
    {
        var callbackInterfaces = handlerType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICallbackHandler<>))
            .ToList();

        if (callbackInterfaces.Count is 0)
        {
            throw new InvalidOperationException($"Type '{handlerType.Name}' does not implement ICallbackHandler<TParams>.");
        }

        var keyProp = handlerType.GetProperty("Key", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Type '{handlerType.Name}' must declare 'public static string Key'.");

        var rawKey = keyProp.GetValue(null) as string;
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            throw new InvalidOperationException($"Type '{handlerType.Name}' declares a null or empty Key trigger.");
        }

        var key = rawKey.Trim();
        services.AddTransient(handlerType);

        for (var i = 0; i < callbackInterfaces.Count; i++)
        {
            var paramsType = callbackInterfaces[i].GetGenericArguments()[0];
            var registration = (CallbackRegistration)CreateRegistrationMethod
                .MakeGenericMethod(handlerType, paramsType)
                .Invoke(null, null)!;

            registry.Register(key, registration);
        }
    }

    internal static CallbackRegistration CreateRegistration<THandler, TParams>()
        where THandler : class, ICallbackHandler<TParams>
        where TParams : class, new()
    {
        return new CallbackRegistration(
            HandlerType: typeof(THandler),
            Factory: static sp => sp.GetRequiredService<THandler>(),
            Invoker: static async (instance, context, sp, ct) =>
            {
                var handler = (THandler)instance;
                var binder = sp.GetRequiredService<ICallbackParameterBinder>();
                var parameters = binder.Bind<TParams>(context);
                await handler.HandleAsync(parameters, context, ct);
            });
    }
}