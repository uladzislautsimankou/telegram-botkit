using System.Reflection;
using System.Runtime.CompilerServices;
using Telegram.BotKit.Commands;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared.Registration;
using Telegram.BotKit.Shared.Startup;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring command handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class CommandServiceCollectionExtensions
{
    /// <summary>
    /// Configures the command pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pipeline is: middleware added before <c>UseRouting()</c>, routing, middleware added after <c>UseRouting()</c>, execution.
    /// If <c>UseRouting()</c> is not called, routing runs before all custom middleware.
    /// Middleware placed after routing runs for every command, including unmatched ones: check
    /// <see cref="CommandContext.RouteResult"/> before relying on the matched handler.
    /// </para>
    /// <para>
    /// The matched handler is created lazily, only when the execution middleware runs, so a middleware that stops the
    /// pipeline after routing never constructs it.
    /// </para>
    /// <para>
    /// Routing needs <see cref="Telegram.BotKit.Hosting.IBotInfo"/>, which is provided by the bot host
    /// (<see cref="Telegram.BotKit.Hosting.BotApplication.CreateBuilder"/>).
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a handler is invalid or its command or alias is already registered by another handler.</exception>
    // NoInlining: if the JIT inlines this method into its caller, Assembly.GetCallingAssembly()
    // returns the caller's caller instead of the application assembly
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddCommands(
        this IServiceCollection services,
        Action<CommandPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new CommandHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new CommandPipelineBuilder(services, registry);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var handlerType in AssemblyScanner.FindTypes(targetAssemblies, CommandRegistrationHelper.IsHandler))
        {
            CommandRegistrationHelper.RegisterHandlerByType(services, registry, handlerType);
        }

        services.AddTransient<CommandRoutingMiddleware>();
        services.AddTransient<CommandExecutionMiddleware>();

        configure?.Invoke(builder);

        registry.Build();
        builder.RegisterPipeline<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<ICommandParameterBinder, CommandParameterBinder>();

        // parameter models are checked when the application is built, not at the first command that uses them
        services.AddSingleton<IStartupCheck>(new ParameterModelStartupCheck(
            "Command",
            () => registry.GetRegistrations().Select(r => (r.HandlerType, r.ParamsType)),
            static sp => sp.GetRequiredService<ICommandParameterBinder>()));

        return services;
    }
}

