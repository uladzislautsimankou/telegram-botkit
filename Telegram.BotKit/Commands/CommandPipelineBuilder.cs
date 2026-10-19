using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the command execution pipeline and register command middleware and handlers.
/// </summary>
public sealed class CommandPipelineBuilder
    : PipelineBuilder<CommandPipelineBuilder, ICommandMiddleware, CommandContext>
{
    private readonly CommandHandlerRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPipelineBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection the pipeline components are registered in.</param>
    /// <param name="registry">The registry that manually added handlers are registered in.</param>
    internal CommandPipelineBuilder(IServiceCollection services, CommandHandlerRegistry registry)
        : base(services)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    internal override Type RoutingMiddlewareType => typeof(CommandRoutingMiddleware);

    /// <inheritdoc />
    internal override Type ExecutionMiddlewareType => typeof(CommandExecutionMiddleware);

    /// <summary>
    /// Manually registers a specific command handler.
    /// Use it for handlers located outside the scanned assemblies; handlers of the scanned assemblies are registered automatically.
    /// </summary>
    /// <remarks>
    /// Registering the same handler type again has no effect.
    /// </remarks>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="ICommandHandler{TParams}"/> exactly once.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the handler is invalid or its command or alias is already registered by another handler.</exception>
    public CommandPipelineBuilder AddHandler<THandler>() where THandler : class
    {
        CommandRegistrationHelper.RegisterHandlerByType(Services, _registry, typeof(THandler));
        return this;
    }
}

