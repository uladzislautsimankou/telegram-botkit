using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the callback execution pipeline and register callback middleware and handlers.
/// </summary>
public sealed class CallbackPipelineBuilder
    : PipelineBuilder<CallbackPipelineBuilder, ICallbackMiddleware, CallbackContext>
{
    private readonly CallbackHandlerRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackPipelineBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection the pipeline components are registered in.</param>
    /// <param name="registry">The registry that manually added handlers are registered in.</param>
    internal CallbackPipelineBuilder(IServiceCollection services, CallbackHandlerRegistry registry)
        : base(services)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    internal override Type RoutingMiddlewareType => typeof(CallbackRoutingMiddleware);

    /// <inheritdoc />
    internal override Type ExecutionMiddlewareType => typeof(CallbackExecutionMiddleware);

    /// <summary>
    /// Manually registers a specific callback handler.
    /// Use it for handlers located outside the scanned assemblies; handlers of the scanned assemblies are registered automatically.
    /// </summary>
    /// <remarks>
    /// Registering the same handler type again has no effect.
    /// </remarks>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="ICallbackHandler{TParams}"/> exactly once.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the handler is invalid or its route key is already registered by another handler.</exception>
    public CallbackPipelineBuilder AddHandler<THandler>() where THandler : class
    {
        CallbackRegistrationHelper.RegisterHandlerByType(Services, _registry, typeof(THandler));
        return this;
    }
}

