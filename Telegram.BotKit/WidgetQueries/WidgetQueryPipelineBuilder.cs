using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Models;
using Telegram.BotKit.WidgetQueries.Routing;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the widget query execution pipeline and register widget query middleware and handlers.
/// </summary>
public sealed class WidgetQueryPipelineBuilder
    : PipelineBuilder<WidgetQueryPipelineBuilder, IWidgetQueryMiddleware, WidgetQueryContext>
{
    private readonly WidgetQueryHandlerRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetQueryPipelineBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection the pipeline components are registered in.</param>
    /// <param name="registry">The registry that manually added handlers are registered in.</param>
    internal WidgetQueryPipelineBuilder(IServiceCollection services, WidgetQueryHandlerRegistry registry)
        : base(services)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    internal override Type RoutingMiddlewareType => typeof(WidgetQueryRoutingMiddleware);

    /// <inheritdoc />
    internal override Type ExecutionMiddlewareType => typeof(WidgetQueryExecutionMiddleware);

    /// <summary>
    /// Manually registers a specific widget query handler.
    /// Use it for handlers located outside the scanned assemblies; handlers of the scanned assemblies are registered automatically.
    /// </summary>
    /// <remarks>
    /// Registering the same handler type again has no effect.
    /// </remarks>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="IWidgetQueryHandler{TParams}"/> exactly once.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the handler is invalid or its route key is already registered by another handler.</exception>
    public WidgetQueryPipelineBuilder AddHandler<THandler>() where THandler : class
    {
        WidgetQueryRegistrationHelper.RegisterHandlerByType(Services, _registry, typeof(THandler));
        return this;
    }
}
