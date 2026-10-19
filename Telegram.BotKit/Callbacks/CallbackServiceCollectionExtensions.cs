using System.Reflection;
using System.Runtime.CompilerServices;
using Telegram.BotKit.Callbacks;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared.Registration;
using Telegram.BotKit.Shared.Startup;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring callback query handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class CallbackServiceCollectionExtensions
{
    /// <summary>
    /// Configures the callback query pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pipeline is: middleware added before <c>UseRouting()</c>, routing, middleware added after <c>UseRouting()</c>, execution.
    /// If <c>UseRouting()</c> is not called, routing runs before all custom middleware.
    /// </para>
    /// <para>
    /// Middleware placed after routing runs even when no handler matched
    /// (<see cref="CallbackContext.MatchedCallbackMetadata"/> is null then; a warning is logged).
    /// The matched handler is created lazily, only when the execution middleware runs.
    /// </para>
    /// <para>
    /// The parameter models of the handlers are validated when the application is built.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a handler is invalid or its route key is already registered by another handler.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddCallbacks(
        this IServiceCollection services,
        Action<CallbackPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new CallbackHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new CallbackPipelineBuilder(services, registry);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var handlerType in AssemblyScanner.FindTypes(targetAssemblies, CallbackRegistrationHelper.IsHandler))
        {
            CallbackRegistrationHelper.RegisterHandlerByType(services, registry, handlerType);
        }

        services.AddTransient<CallbackRoutingMiddleware>();
        services.AddTransient<CallbackExecutionMiddleware>();

        configure?.Invoke(builder);

        registry.Build();
        builder.RegisterPipeline<ICallbackDispatcher, CallbackDispatcher>();
        services.AddSingleton<ICallbackParameterBinder, CallbackParameterBinder>();

        // parameter models are checked when the application is built, not at the first callback that uses them
        services.AddSingleton<IStartupCheck>(new ParameterModelStartupCheck(
            "Callback",
            () => registry.GetRegistrations().Select(r => (r.HandlerType, r.ParamsType)),
            static sp => sp.GetRequiredService<ICallbackParameterBinder>()));

        return services;
    }
}

