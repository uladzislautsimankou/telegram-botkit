using System.Reflection;
using System.Runtime.CompilerServices;
using Telegram.BotKit.Messages;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring standard message handling and pipeline registration on <see cref="IServiceCollection"/>.
/// </summary>
public static class MessageServiceCollectionExtensions
{
    /// <summary>
    /// Configures the message pipeline and automatically registers all message handlers from the specified assemblies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pipeline is: middleware added before <c>UseRouting()</c>, routing, middleware added after <c>UseRouting()</c>, execution.
    /// If <c>UseRouting()</c> is not called, routing runs before all custom middleware, which means
    /// <see cref="MessageContext.UserState"/> set by a middleware is not visible to routing: put such middleware
    /// before an explicit <c>UseRouting()</c>.
    /// </para>
    /// <para>
    /// Routing first looks for handlers bound to the current user state, then for state-agnostic ones.
    /// Middleware placed after routing runs even when no handler matched
    /// (<see cref="MessageContext.MatchedMessageMetadata"/> is null then).
    /// The matched handler is created lazily, only when the execution middleware runs.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to add components to.</param>
    /// <param name="configure">An optional configuration callback to customize the middleware pipeline.</param>
    /// <param name="assemblies">The assemblies to scan for handlers. If omitted, uses the calling assembly.</param>
    /// <returns>The service collection instance for method chaining.</returns>
    // NoInlining: if the JIT inlines this method into its caller, Assembly.GetCallingAssembly()
    // returns the caller's caller instead of the application assembly
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddMessages(
        this IServiceCollection services,
        Action<MessagePipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var registry = new MessageHandlerRegistry();
        services.AddSingleton(registry);

        var builder = new MessagePipelineBuilder(services, registry);
        var targetAssemblies = assemblies.Length is 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        foreach (var handlerType in AssemblyScanner.FindTypes(targetAssemblies, MessageRegistrationHelper.IsHandler))
        {
            MessageRegistrationHelper.RegisterHandlerByType(services, registry, handlerType);
        }

        services.AddTransient<MessageRoutingMiddleware>();
        services.AddTransient<MessageExecutionMiddleware>();

        configure?.Invoke(builder);

        registry.Build();
        builder.RegisterPipeline<IMessageDispatcher, MessageDispatcher>();

        return services;
    }
}

