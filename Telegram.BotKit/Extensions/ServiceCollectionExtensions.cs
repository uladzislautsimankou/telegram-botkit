using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using Telegram.Bot.Polling;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Invocation;

namespace Telegram.BotKit.Extensions;

/// <summary>
/// Extension methods for setting up Telegram.BotKit services in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the specified assembly for implementations of <see cref="ICommandHandler{TParams}"/> and registers them.
    /// Handlers and their invokers are registered with a <see cref="ServiceLifetime.Transient"/> lifetime.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="assembly">The assembly to scan for handler implementations.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddCommandHandlers(this IServiceCollection services, Assembly assembly)
    {
        AddHandlers(
            services,
            assembly,
            genericInterfaceType: typeof(ICommandHandler<>),
            genericInvokerType: typeof(CommandHandlerInvoker<>),
            invokerServiceType: typeof(ICommandHandlerInvoker)
        );

        return services;
    }

    /// <summary>
    /// Scans the specified assembly for implementations of <see cref="ICallbackHandler{TParams}"/> and registers them.
    /// Handlers and their invokers are registered with a <see cref="ServiceLifetime.Transient"/> lifetime.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="assembly">The assembly to scan for handler implementations.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddCallbackHandlers(this IServiceCollection services, Assembly assembly)
    {
        AddHandlers(
            services,
            assembly,
            genericInterfaceType: typeof(ICallbackHandler<>),
            genericInvokerType: typeof(CallbackHandlerInvoker<>),
            invokerServiceType: typeof(ICallbackHandlerInvoker)
        );

        return services;
    }

    /// <summary>
    /// Replaces the default <see cref="IUpdateHandler"/> implementation with a custom one.
    /// Use this if you need to completely override the update processing logic.
    /// </summary>
    /// <typeparam name="T">The type of the custom update handler implementation.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection ReplaceUpdateHandler<T>(this IServiceCollection services)
        where T : class, IUpdateHandler
    {
        services.RemoveAll<IUpdateHandler>();
        services.AddSingleton<IUpdateHandler, T>();
        return services;
    }

    private static void AddHandlers(
        IServiceCollection services,
        Assembly assembly,
        Type genericInterfaceType,
        Type genericInvokerType,
        Type invokerServiceType)
    {
        // Scan assembly for concrete implementations excluding abstracts and interfaces
        var handlerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType)
                .Select(i => new { Interface = i, Implementation = t }))
            .ToList();

        foreach (var typeInfo in handlerTypes)
        {
            // Register the handler implementation itself
            services.AddTransient(typeInfo.Interface, typeInfo.Implementation);

            // Construct the Generic Invoker type
            // Extract TParams from IHandler<TParams>
            var paramsType = typeInfo.Interface.GetGenericArguments()[0];

            // Create HandlerInvoker<TParams> type
            var concreteInvokerType = genericInvokerType.MakeGenericType(paramsType);

            // Register the Invoker using ActivatorUtilities to inject dependencies automatically
            services.AddTransient(invokerServiceType, sp =>
            {
                var handler = sp.GetRequiredService(typeInfo.Interface);
                return ActivatorUtilities.CreateInstance(sp, concreteInvokerType, handler);
            });
        }
    }
}