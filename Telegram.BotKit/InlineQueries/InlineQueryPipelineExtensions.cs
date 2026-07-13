using Microsoft.Extensions.Logging;
using System.Reflection;
using Telegram.Bot;
using Telegram.BotKit.InlineQueries;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class InlineQueryPipelineBuilder(IServiceCollection services)
    : PipelineBuilder<IInlineQueryMiddleware>(services)
{
    /// <summary>
    /// Manually registers a specific inline query handler.
    /// </summary>
    public InlineQueryPipelineBuilder AddHandler<THandler>() where THandler : class, IInlineQueryHandler
    {
        Services.AddTransient<IInlineQueryHandler, THandler>();
        return this;
    }
}

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures the inline query pipeline and automatically registers all handlers from assemblies.
    /// </summary>
    public static IServiceCollection AddInlineQueries(
        this IServiceCollection services,
        Action<InlineQueryPipelineBuilder>? configure = null,
        params Assembly[] assemblies)
    {
        var builder = new InlineQueryPipelineBuilder(services);

        // scan assembly if user has not explicitly passed custom ones
        var targetAssemblies = assemblies.Length is 0
            ? [Assembly.GetCallingAssembly()]
            : assemblies;

        foreach (var assembly in targetAssemblies)
        {
            RegisterHandlersFromAssembly(services, assembly);
        }

        // register internal system routing middleware
        services.AddTransient<InlineQueryRoutingMiddleware>();

        // let the user configure custom middlewares
        configure?.Invoke(builder);

        // insert system routing middleware as the final pipeline step
        builder.UseMiddleware<InlineQueryRoutingMiddleware>();

        // register pipeline builder and dispatcher
        services.AddSingleton(builder);

        services.AddSingleton<IInlineQueryDispatcher, InlineQueryDispatcher>();

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var handlerType = typeof(IInlineQueryHandler);

        var concreteHandlers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && handlerType.IsAssignableFrom(t));

        foreach (var type in concreteHandlers)
        {
            services.AddTransient(handlerType, type);
        }
    }
}
