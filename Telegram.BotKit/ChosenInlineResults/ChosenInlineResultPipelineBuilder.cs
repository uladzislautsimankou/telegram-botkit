using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.ChosenInlineResults.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the chosen inline result execution pipeline and register middleware and handlers.
/// </summary>
public sealed class ChosenInlineResultPipelineBuilder
    : PipelineBuilder<ChosenInlineResultPipelineBuilder, IChosenInlineResultMiddleware, ChosenInlineResultContext>
{
    private readonly ChosenInlineResultHandlerRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChosenInlineResultPipelineBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection the pipeline components are registered in.</param>
    /// <param name="registry">The registry that manually added handlers are registered in.</param>
    internal ChosenInlineResultPipelineBuilder(IServiceCollection services, ChosenInlineResultHandlerRegistry registry)
        : base(services)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    internal override Type RoutingMiddlewareType => typeof(ChosenInlineResultRoutingMiddleware);

    /// <inheritdoc />
    internal override Type ExecutionMiddlewareType => typeof(ChosenInlineResultExecutionMiddleware);

    /// <summary>
    /// Manually registers a specific chosen inline result handler.
    /// Use it for handlers located outside the scanned assemblies; handlers of the scanned assemblies are registered automatically.
    /// </summary>
    /// <remarks>
    /// Registering the same handler type again has no effect. The first handler whose
    /// <see cref="IChosenInlineResultHandler.CanHandle"/> returns true processes the result;
    /// see <see cref="IChosenInlineResultHandler.Order"/> for the evaluation order.
    /// </remarks>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="IChosenInlineResultHandler"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public ChosenInlineResultPipelineBuilder AddHandler<THandler>() where THandler : class, IChosenInlineResultHandler
    {
        ChosenInlineResultRegistrationHelper.RegisterHandler<THandler>(
            Services,
            _registry);

        return this;
    }
}

