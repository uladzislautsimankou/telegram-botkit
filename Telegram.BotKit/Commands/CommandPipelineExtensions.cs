using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.BotKit.Commands;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the command execution pipeline and register command middleware and handlers.
/// </summary>
public sealed class CommandPipelineBuilder(IServiceCollection services)
    : PipelineBuilder<ICommandMiddleware, CommandContext>(services)
{
    /// <summary>
    /// Explicitly defines the routing position in the pipeline.
    /// If routing is already configured, subsequent calls are ignored.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public CommandPipelineBuilder UseRouting()
    {
        // prevent duplicate routing steps in the pipeline
        if (Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            return this;
        }

        Steps.Add(new PipelineStep(PipelineStepType.Routing, typeof(CommandRoutingMiddleware)));
        return this;
    }

    /// <summary>
    /// Manually registers a specific command handler and precompiles its factory and invoker.
    /// </summary>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="ICommandHandler{TParams}"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public CommandPipelineBuilder AddHandler<THandler>() where THandler : class
    {
        var registry = GetOrCreateRegistry();
        CommandRegistrationHelper.RegisterHandler(Services, registry, typeof(THandler));
        return this;
    }

    private CommandHandlerRegistry GetOrCreateRegistry()
    {
        var registryDescriptor = Services.FirstOrDefault(d => d.ServiceType == typeof(CommandHandlerRegistry));
        if (registryDescriptor?.ImplementationInstance is CommandHandlerRegistry r)
        {
            return r;
        }

        var registry = new CommandHandlerRegistry();
        Services.AddSingleton(registry);
        return registry;
    }
}

/// <summary>
/// Extension methods for configuring command handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class CommandServiceCollectionExtensions
{
    /// <summary>
    /// Configures the command pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    public static IServiceCollection AddCommands(
        this IServiceCollection services,
        Action<CommandPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new CommandHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new CommandPipelineBuilder(services);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var assembly in targetAssemblies)
        {
            RegisterHandlersFromAssembly(services, assembly, registry);
        }

        services.AddTransient<CommandRoutingMiddleware>();
        services.AddTransient<CommandExecutionMiddleware>();

        configure?.Invoke(builder);

        registry.Build();

        if (builder.Steps.Count is 0)
        {
            builder.UseRouting();
        }
        else if (!builder.Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            builder.Steps.Insert(0, new PipelineStep(PipelineStepType.Routing, typeof(CommandRoutingMiddleware)));
        }

        builder.Steps.Add(new PipelineStep(PipelineStepType.Execution, typeof(CommandExecutionMiddleware)));

        var compiledPipeline = builder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        services.AddSingleton(compiledPipeline);
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<ICommandParameterBinder, CommandParameterBinder>();

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly, CommandHandlerRegistry registry)
    {
        var genericInterfaceType = typeof(ICommandHandler<>);

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
                CommandRegistrationHelper.RegisterHandler(services, registry, type);
            }
        }
    }
}

internal static class CommandRegistrationHelper
{
    private static readonly MethodInfo CreateRegistrationMethod = typeof(CommandRegistrationHelper)
        .GetMethod(nameof(CreateRegistration), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static void RegisterHandler(IServiceCollection services, CommandHandlerRegistry registry, Type handlerType)
    {
        var commandInterfaces = handlerType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
            .ToList();

        if (commandInterfaces.Count is 0)
        {
            throw new InvalidOperationException($"Type '{handlerType.Name}' does not implement ICommandHandler<TParams>.");
        }

        var commandProp = handlerType.GetProperty("Command", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Type '{handlerType.Name}' must declare 'public static string Command'.");

        var rawCommand = commandProp.GetValue(null) as string;
        if (string.IsNullOrWhiteSpace(rawCommand))
        {
            throw new InvalidOperationException($"Type '{handlerType.Name}' declares a null or empty Command trigger.");
        }
        var command = rawCommand.TrimStart('/');

        var aliasProp = handlerType.GetProperty("Aliases", BindingFlags.Public | BindingFlags.Static);
        var aliases = ((string[]?)aliasProp?.GetValue(null) ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.TrimStart('/'))
            .ToArray();

        services.TryAddTransient(handlerType);

        for (var i = 0; i < commandInterfaces.Count; i++)
        {
            var paramsType = commandInterfaces[i].GetGenericArguments()[0];
            var registration = (CommandRegistration)CreateRegistrationMethod
                .MakeGenericMethod(handlerType, paramsType)
                .Invoke(null, null)!;

            registry.Register(command, aliases, registration);
        }
    }

    internal static CommandRegistration CreateRegistration<THandler, TParams>()
        where THandler : class, ICommandHandler<TParams>
        where TParams : class, new()
    {
        return new CommandRegistration(
            HandlerType: typeof(THandler),
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
