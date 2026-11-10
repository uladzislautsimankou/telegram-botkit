using System.Reflection;
using System.Runtime.CompilerServices;
using Telegram.BotKit.Shared.Registration;
using Telegram.BotKit.Shared.Startup;
using Telegram.BotKit.WidgetQueries;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Binding;
using Telegram.BotKit.WidgetQueries.Models;
using Telegram.BotKit.WidgetQueries.Routing;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring widget query handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class WidgetQueryServiceCollectionExtensions
{
    /// <summary>
    /// Configures the widget query pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pipeline is: middleware added before <c>UseRouting()</c>, routing, middleware added after <c>UseRouting()</c>, execution.
    /// If <c>UseRouting()</c> is not called, routing runs before all custom middleware.
    /// </para>
    /// <para>
    /// Middleware placed after routing runs even when no handler matched
    /// (<see cref="WidgetQueryContext.MatchedWidgetQueryMetadata"/> is null then; a warning is logged).
    /// The matched handler is created lazily, only when the execution middleware runs.
    /// </para>
    /// <para>
    /// The parameter models of the handlers are validated when the application is built.
    /// </para>
    /// <para>
    /// Both kinds of widget query (<c>widget_query</c> and <c>shared_widget_query</c>) go through this one pipeline.
    /// The application's update handler passes them to <see cref="IWidgetQueryDispatcher"/>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a handler is invalid or its route key is already registered by another handler.</exception>
    // NoInlining: if the JIT inlines this method into its caller, Assembly.GetCallingAssembly()
    // returns the caller's caller instead of the application assembly
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddWidgetQueries(
        this IServiceCollection services,
        Action<WidgetQueryPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new WidgetQueryHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new WidgetQueryPipelineBuilder(services, registry);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var handlerType in AssemblyScanner.FindTypes(targetAssemblies, WidgetQueryRegistrationHelper.IsHandler))
        {
            WidgetQueryRegistrationHelper.RegisterHandlerByType(services, registry, handlerType);
        }

        services.AddTransient<WidgetQueryRoutingMiddleware>();
        services.AddTransient<WidgetQueryExecutionMiddleware>();

        configure?.Invoke(builder);

        registry.Build();
        builder.RegisterPipeline<IWidgetQueryDispatcher, WidgetQueryDispatcher>();
        services.AddSingleton<IWidgetParameterBinder, WidgetParameterBinder>();

        // parameter models are checked when the application is built, not at the first query that uses them
        services.AddSingleton<IStartupCheck>(new ParameterModelStartupCheck(
            "Widget query",
            () => registry.GetRegistrations().Select(r => (r.HandlerType, r.ParamsType)),
            static sp => sp.GetRequiredService<IWidgetParameterBinder>()));

        return services;
    }
}
