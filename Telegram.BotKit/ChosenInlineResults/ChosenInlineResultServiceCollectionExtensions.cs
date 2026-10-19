using System.Reflection;
using System.Runtime.CompilerServices;
using Telegram.BotKit.ChosenInlineResults;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.ChosenInlineResults.Routing;
using Telegram.BotKit.Shared.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring chosen inline result handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class ChosenInlineResultServiceCollectionExtensions
{
    /// <summary>
    /// Configures the chosen inline result pipeline and automatically registers all handlers from the specified assemblies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pipeline is: middleware added before <c>UseRouting()</c>, routing, middleware added after <c>UseRouting()</c>, execution.
    /// If <c>UseRouting()</c> is not called, routing runs before all custom middleware.
    /// </para>
    /// <para>
    /// Middleware placed after routing runs even when no handler matched
    /// (<see cref="ChosenInlineResultContext.MatchedChosenInlineResultMetadata"/> is null then).
    /// The matched handler is created lazily, only when the execution middleware runs.
    /// </para>
    /// <para>
    /// Telegram sends these updates only when inline feedback is enabled for the bot in BotFather.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    // NoInlining: if the JIT inlines this method into its caller, Assembly.GetCallingAssembly()
    // returns the caller's caller instead of the application assembly
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddChosenInlineResults(
        this IServiceCollection services,
        Action<ChosenInlineResultPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new ChosenInlineResultHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new ChosenInlineResultPipelineBuilder(services, registry);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var handlerType in AssemblyScanner.FindTypes(targetAssemblies, ChosenInlineResultRegistrationHelper.IsHandler))
        {
            ChosenInlineResultRegistrationHelper.RegisterHandlerByType(services, registry, handlerType);
        }

        services.AddTransient<ChosenInlineResultRoutingMiddleware>();
        services.AddTransient<ChosenInlineResultExecutionMiddleware>();

        configure?.Invoke(builder);

        registry.Build();
        builder.RegisterPipeline<IChosenInlineResultDispatcher, ChosenInlineResultDispatcher>();

        return services;
    }
}

