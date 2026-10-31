using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Validates callback handlers and registers them in the registry and the dependency injection container.
/// </summary>
internal static class CallbackRegistrationHelper
{
    /// <summary>
    /// Determines whether the type implements <see cref="ICallbackHandler{TParams}"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements <see cref="ICallbackHandler{TParams}"/>; otherwise, false.</returns>
    public static bool IsHandler(Type type) => AssemblyScanner.Implements(type, typeof(ICallbackHandler<>));

    /// <summary>
    /// Registers a handler known only by its runtime type: validates it and continues in the generic <see cref="RegisterCore{THandler,TParams}"/>.
    /// </summary>
    /// <param name="services">The service collection the handler is registered in.</param>
    /// <param name="registry">The registry that maps route keys to handler registrations.</param>
    /// <param name="handlerType">The runtime type of the handler.</param>
    /// <exception cref="InvalidOperationException">Thrown when the type does not implement <see cref="ICallbackHandler{TParams}"/> exactly once, declares an invalid route key, or the key is already registered by another handler.</exception>
    public static void RegisterHandlerByType(IServiceCollection services, CallbackHandlerRegistry registry, Type handlerType)
    {
        var interfaces = handlerType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICallbackHandler<>))
            .ToArray();

        if (interfaces.Length is 0)
        {
            throw new InvalidOperationException($"Type '{handlerType.Name}' does not implement ICallbackHandler<TParams>.");
        }

        if (interfaces.Length > 1)
        {
            throw new InvalidOperationException(
                $"Type '{handlerType.Name}' implements ICallbackHandler<> {interfaces.Length} times. " +
                "A handler is bound to a single route key, so it must implement exactly one parameter model.");
        }

        GenericMethodInvoker.Invoke(
            typeof(CallbackRegistrationHelper),
            nameof(RegisterCore),
            [handlerType, interfaces[0].GetGenericArguments()[0]],
            services,
            registry);
    }

    private static void RegisterCore<THandler, TParams>(IServiceCollection services, CallbackHandlerRegistry registry)
        where THandler : class, ICallbackHandler<TParams>
        where TParams : class, new()
    {
        var key = THandler.Key?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException($"Type '{typeof(THandler).Name}' declares a null or empty Key trigger.");
        }

        // '?' separates the route key from parameters in callback data, so such a key would never match
        if (key.Contains('?'))
        {
            throw new InvalidOperationException($"Key '{key}' of '{typeof(THandler).Name}' must not contain '?'.");
        }

        services.TryAddTransient<THandler>();
        registry.Register(key, CreateRegistration<THandler, TParams>());
    }

    /// <summary>
    /// Creates the compiled factory and invoker (parameter binding followed by the handler call) of a handler.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <typeparam name="TParams">The parameter model of the handler.</typeparam>
    /// <returns>The registration of the handler.</returns>
    internal static CallbackRegistration CreateRegistration<THandler, TParams>()
        where THandler : class, ICallbackHandler<TParams>
        where TParams : class, new()
    {
        return new CallbackRegistration(
            HandlerType: typeof(THandler),
            ParamsType: typeof(TParams),
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

