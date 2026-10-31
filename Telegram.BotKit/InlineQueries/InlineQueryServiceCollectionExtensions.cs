using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.BotKit.InlineQueries;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Middlewares;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.InlineQueries.Routing;
using Telegram.BotKit.Shared.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring inline query handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class InlineQueryServiceCollectionExtensions
{
    /// <summary>
    /// Configures the inline query pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pipeline is: middleware added before <c>UseRouting()</c>, routing, middleware added after <c>UseRouting()</c>, answer.
    /// If <c>UseRouting()</c> is not called, routing runs before all custom middleware.
    /// Routing runs every matching handler and aggregates their results into the context;
    /// the answer middleware (the default one unless replaced with <c>UseAnswer</c>) sends them to Telegram.
    /// </para>
    /// <para>
    /// A failing handler does not break the others: its exception is collected in
    /// <see cref="InlineQueryContext.Exceptions"/> and logged.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    // NoInlining: if the JIT inlines this method into its caller, Assembly.GetCallingAssembly()
    // returns the caller's caller instead of the application assembly
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddInlineQueries(
        this IServiceCollection services,
        Action<InlineQueryPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new InlineQueryHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new InlineQueryPipelineBuilder(services, registry);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var handlerType in AssemblyScanner.FindTypes(targetAssemblies, InlineQueryRegistrationHelper.IsHandler))
        {
            InlineQueryRegistrationHelper.RegisterHandlerByType(services, registry, handlerType);
        }

        services.AddTransient<InlineQueryRoutingMiddleware>();
        services.TryAddTransient<IInlineQueryAnswerMiddleware, DefaultInlineQueryAnswerMiddleware>();

        configure?.Invoke(builder);

        registry.Build();
        services.AddSingleton(new InlineQueryPipelineSettings(builder.ExecutionMode));
        builder.RegisterPipeline<IInlineQueryDispatcher, InlineQueryDispatcher>();

        return services;
    }
}

