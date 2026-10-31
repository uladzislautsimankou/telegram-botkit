using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Validates command handlers and registers them in the registry and the dependency injection container.
/// </summary>
internal static class CommandRegistrationHelper
{
    /// <summary>
    /// Determines whether the type implements <see cref="ICommandHandler{TParams}"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements <see cref="ICommandHandler{TParams}"/>; otherwise, false.</returns>
    public static bool IsHandler(Type type) => AssemblyScanner.Implements(type, typeof(ICommandHandler<>));

    /// <summary>
    /// Registers a handler known only by its runtime type: validates it and continues in the generic <see cref="RegisterCore{THandler,TParams}"/>.
    /// </summary>
    /// <param name="services">The service collection the handler is registered in.</param>
    /// <param name="registry">The registry that maps command triggers and aliases to handler registrations.</param>
    /// <param name="handlerType">The runtime type of the handler.</param>
    /// <exception cref="InvalidOperationException">Thrown when the type does not implement <see cref="ICommandHandler{TParams}"/> exactly once, declares an invalid trigger or alias, or one of them is already registered by another handler.</exception>
    public static void RegisterHandlerByType(IServiceCollection services, CommandHandlerRegistry registry, Type handlerType)
    {
        var interfaces = handlerType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
            .ToArray();

        if (interfaces.Length is 0)
        {
            throw new InvalidOperationException($"Type '{handlerType.Name}' does not implement ICommandHandler<TParams>.");
        }

        // Command and Aliases are static, so one class can only have one trigger
        if (interfaces.Length > 1)
        {
            throw new InvalidOperationException(
                $"Type '{handlerType.Name}' implements ICommandHandler<> {interfaces.Length} times. " +
                "A handler is bound to a single command trigger, so it must implement exactly one parameter model.");
        }

        GenericMethodInvoker.Invoke(
            typeof(CommandRegistrationHelper),
            nameof(RegisterCore),
            [handlerType, interfaces[0].GetGenericArguments()[0]],
            services,
            registry);
    }

    private static void RegisterCore<THandler, TParams>(IServiceCollection services, CommandHandlerRegistry registry)
        where THandler : class, ICommandHandler<TParams>
        where TParams : class, new()
    {
        var command = THandler.Command?.Trim().TrimStart('/');
        if (string.IsNullOrEmpty(command))
        {
            throw new InvalidOperationException($"Type '{typeof(THandler).Name}' declares a null or empty Command trigger.");
        }

        ValidateTrigger(command, typeof(THandler));

        var aliases = (THandler.Aliases ?? [])
            .Select(a => a?.Trim().TrimStart('/'))
            .Where(a => !string.IsNullOrEmpty(a))
            .Select(a => a!)
            .ToArray();

        foreach (var alias in aliases)
        {
            ValidateTrigger(alias, typeof(THandler));
        }

        services.TryAddTransient<THandler>();
        registry.Register(command, aliases, CreateRegistration<THandler, TParams>());
    }

    // the parser splits the command name on whitespace and '@', so such triggers could never match
    private static void ValidateTrigger(string trigger, Type handlerType)
    {
        if (trigger.Any(c => char.IsWhiteSpace(c) || c is '@'))
        {
            throw new InvalidOperationException(
                $"Command trigger '{trigger}' of '{handlerType.Name}' must not contain whitespace or '@'.");
        }
    }

    /// <summary>
    /// Creates the compiled factory and invoker (parameter binding followed by the handler call) of a handler.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <typeparam name="TParams">The parameter model of the handler.</typeparam>
    /// <returns>The registration of the handler.</returns>
    internal static CommandRegistration CreateRegistration<THandler, TParams>()
        where THandler : class, ICommandHandler<TParams>
        where TParams : class, new()
    {
        return new CommandRegistration(
            HandlerType: typeof(THandler),
            ParamsType: typeof(TParams),
            Factory: static sp => sp.GetRequiredService<THandler>(),
            Invoker: static async (instance, context, sp, ct) =>
            {
                var handler = (THandler)instance;
                var binder = sp.GetRequiredService<ICommandParameterBinder>();
                var parameters = binder.Bind<TParams>(context);
                await handler.HandleAsync(parameters, context, ct);
            });
    }
}

