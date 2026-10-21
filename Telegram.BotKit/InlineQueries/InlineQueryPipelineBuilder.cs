using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Middlewares;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.InlineQueries.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder used to configure the inline query execution pipeline and register middleware and handlers.
/// </summary>
public sealed class InlineQueryPipelineBuilder
    : PipelineBuilder<InlineQueryPipelineBuilder, IInlineQueryMiddleware, InlineQueryContext>
{
    private readonly InlineQueryHandlerRegistry _registry;
    private Type? _answerMiddlewareType;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineQueryPipelineBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection the pipeline components are registered in.</param>
    /// <param name="registry">The registry that manually added handlers are registered in.</param>
    internal InlineQueryPipelineBuilder(IServiceCollection services, InlineQueryHandlerRegistry registry)
        : base(services)
    {
        _registry = registry;
    }

    /// <summary>
    /// Gets the mode in which several handlers matching the same inline query are executed.
    /// </summary>
    internal InlineQueryExecutionMode ExecutionMode { get; private set; } = InlineQueryExecutionMode.Parallel;

    /// <inheritdoc />
    internal override Type RoutingMiddlewareType => typeof(InlineQueryRoutingMiddleware);

    /// <inheritdoc />
    // the answer middleware is the execution step of this pipeline; the default one is resolved through its interface
    internal override Type ExecutionMiddlewareType => _answerMiddlewareType ?? typeof(IInlineQueryAnswerMiddleware);

    /// <summary>
    /// Replaces the default answer middleware, the last step of the pipeline that sends the aggregated results to Telegram.
    /// Derive from <see cref="DefaultInlineQueryAnswerMiddleware"/> to customize only a part of its behavior.
    /// </summary>
    /// <remarks>
    /// Only one answer middleware is used: subsequent calls are ignored.
    /// The answer middleware always runs last, wherever in the configuration this method is called.
    /// </remarks>
    /// <typeparam name="TMiddleware">The answer middleware type.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public InlineQueryPipelineBuilder UseAnswer<TMiddleware>()
        where TMiddleware : class, IInlineQueryAnswerMiddleware
    {
        if (_answerMiddlewareType is not null)
        {
            return this;
        }

        // the execution step itself is added last, when the pipeline is completed (see ExecutionMiddlewareType),
        // so middleware and routing configured after this call still run before the answer
        Services.TryAddTransient<TMiddleware>();
        _answerMiddlewareType = typeof(TMiddleware);
        return this;
    }

    /// <summary>
    /// Defines how several handlers matching the same inline query are executed.
    /// Defaults to <see cref="InlineQueryExecutionMode.Parallel"/>.
    /// </summary>
    /// <param name="mode">The execution mode.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InlineQueryPipelineBuilder WithExecutionMode(InlineQueryExecutionMode mode)
    {
        ExecutionMode = mode;
        return this;
    }

    /// <summary>
    /// Manually registers a specific inline query handler.
    /// Use it for handlers located outside the scanned assemblies; handlers of the scanned assemblies are registered automatically.
    /// </summary>
    /// <remarks>
    /// Registering the same handler type again has no effect. All handlers whose
    /// <see cref="IInlineQueryHandler.CanHandle"/> returns true run, and their results are aggregated
    /// in the order defined by <see cref="IInlineQueryHandler.Order"/>.
    /// </remarks>
    /// <typeparam name="THandler">The concrete handler type implementing <see cref="IInlineQueryHandler"/>.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    public InlineQueryPipelineBuilder AddHandler<THandler>() where THandler : class, IInlineQueryHandler
    {
        InlineQueryRegistrationHelper.RegisterHandler<THandler>(Services, _registry);
        return this;
    }
}

