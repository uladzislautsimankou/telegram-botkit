using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the message execution pipeline and register message middleware and handlers.
/// </summary>
public sealed class MessagePipelineBuilder
    : PipelineBuilder<MessagePipelineBuilder, IMessageMiddleware, MessageContext>
{
    private readonly MessageHandlerRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePipelineBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection the pipeline components are registered in.</param>
    /// <param name="registry">The registry that manually added handlers are registered in.</param>
    internal MessagePipelineBuilder(IServiceCollection services, MessageHandlerRegistry registry)
        : base(services)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    internal override Type RoutingMiddlewareType => typeof(MessageRoutingMiddleware);

    /// <inheritdoc />
    internal override Type ExecutionMiddlewareType => typeof(MessageExecutionMiddleware);

    /// <summary>
    /// Manually registers a specific message handler.
    /// Use it for handlers located outside the scanned assemblies; handlers of the scanned assemblies are registered automatically.
    /// </summary>
    /// <remarks>
    /// Registering the same handler type again has no effect. Among the handlers of a user state the first
    /// one whose <see cref="IMessageHandler.CanHandle"/> returns true processes the message;
    /// see <see cref="IMessageHandler.Order"/> for the evaluation order.
    /// </remarks>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="IMessageHandler"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public MessagePipelineBuilder AddHandler<THandler>() where THandler : class, IMessageHandler
    {
        MessageRegistrationHelper.RegisterHandler<THandler>(Services, _registry);
        return this;
    }
}

