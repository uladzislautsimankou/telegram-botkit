using System.Reflection;
using Telegram.BotKit.Commands;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class CommandPipelineBuilder(IServiceCollection services) 
    : PipelineBuilder<ICommandMiddleware>(services)
{
    /// <summary>
    /// Explicitly defines the routing position in the pipeline.
    /// </summary>
    public CommandPipelineBuilder UseRouting()
    {
        Steps.Add(new PipelineStep(PipelineStepType.Routing, typeof(CommandRoutingMiddleware)));
        return this;
    }

    /// <summary>
    /// Manually registers a specific command handler and registers its corresponding invoker.
    /// </summary>
    public CommandPipelineBuilder AddHandler<THandler>() where THandler : class
    {
        var handlerType = typeof(THandler);
        var registry = GetOrCreateRegistry();

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
        var command = (string)commandProp.GetValue(null)!;

        var aliasProp = handlerType.GetProperty("Aliases", BindingFlags.Public | BindingFlags.Static);
        var aliases = (string[]?)aliasProp?.GetValue(null) ?? [];

        foreach (var commandInterface in commandInterfaces)
        {
            // register the handler itself under its interface ICommandHandler<TParams>
            Services.AddTransient(commandInterface, handlerType);

            // extract TParams
            var paramsType = commandInterface.GetGenericArguments()[0];

            // dynamically create the CommandHandlerInvoker<TParams> type
            var concreteInvokerType = typeof(CommandHandlerInvoker<>).MakeGenericType(paramsType);

            // register the type in DI and add it to the registry
            Services.AddTransient(concreteInvokerType);
            registry.Register(command, aliases, concreteInvokerType);
        }

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

public static class CommandServiceCollectionExtensions
{
    /// <summary>
    /// Configures the command pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    public static IServiceCollection AddCommands(
        this IServiceCollection services,
        Action<CommandPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new CommandHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new CommandPipelineBuilder(services);

        // automatically scan assemblies, register handlers, and generate invokers for them
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;
        foreach (var assembly in targetAssemblies)
        {
            RegisterHandlersAndInvokers(services, assembly, registry);
        }

        // register required system services
        services.AddTransient<CommandRoutingMiddleware>();
        services.AddTransient<CommandExecutionMiddleware>();

        // configure user pipeline
        configure?.Invoke(builder);

        // default settings if the pipeline is empty
        if (builder.Steps.Count is 0)
        {
            builder.UseRouting();
        }
        else if (!builder.Steps.Any(s => s.Type == PipelineStepType.Routing))
        {
            builder.Steps.Insert(0, new PipelineStep(PipelineStepType.Routing, typeof(CommandRoutingMiddleware)));
        }

        // executor is always last
        builder.Steps.Add(new PipelineStep(PipelineStepType.Execution, typeof(CommandExecutionMiddleware)));

        services.AddSingleton(builder);
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddTransient<ICommandParameterBinder, CommandParameterBinder>();

        return services;
    }

    private static void RegisterHandlersAndInvokers(IServiceCollection services, Assembly assembly, CommandHandlerRegistry registry)
    {
        var genericInterfaceType = typeof(ICommandHandler<>);
        var genericInvokerType = typeof(CommandHandlerInvoker<>);

        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType)
                .Select(i => new { Interface = i, Implementation = t }))
            .ToList();

        foreach (var typeInfo in handlerTypes)
        {
            var handlerType = typeInfo.Implementation;

            var commandProp = handlerType.GetProperty("Command", BindingFlags.Public | BindingFlags.Static);
            var command = (string)commandProp!.GetValue(null)!;

            var aliasProp = handlerType.GetProperty("Aliases", BindingFlags.Public | BindingFlags.Static);
            var aliases = (string[]?)aliasProp?.GetValue(null) ?? [];

            services.AddTransient(typeInfo.Interface, handlerType);

            var paramsType = typeInfo.Interface.GetGenericArguments()[0];
            var concreteInvokerType = genericInvokerType.MakeGenericType(paramsType);
            services.AddTransient(concreteInvokerType);

            registry.Register(command, aliases, concreteInvokerType);
        }
    }
}