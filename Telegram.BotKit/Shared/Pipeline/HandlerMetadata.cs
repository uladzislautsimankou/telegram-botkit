namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Describes the handler matched by the routing middleware.
/// The handler instance is created lazily on the first access to <see cref="HandlerInstance"/> (normally by the
/// execution middleware), so a middleware that stops the pipeline after routing (authorization, rate limiting, ...)
/// never pays for constructing the handler and its dependencies.
/// </summary>
public abstract class HandlerMetadata
{
    private readonly Func<object> _instanceFactory;
    private object? _instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerMetadata"/> class.
    /// </summary>
    /// <param name="handlerType">The concrete type of the matched handler.</param>
    /// <param name="instanceFactory">A factory creating the handler instance; invoked at most once.</param>
    protected HandlerMetadata(Type handlerType, Func<object> instanceFactory)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(instanceFactory);

        HandlerType = handlerType;
        _instanceFactory = instanceFactory;
    }

    /// <summary>
    /// Gets the concrete type of the matched handler. Reading it does not create the handler.
    /// </summary>
    public Type HandlerType { get; }

    /// <summary>
    /// Gets the matched handler instance, creating it on first access.
    /// Not thread-safe: a context is processed sequentially by its pipeline.
    /// </summary>
    public object HandlerInstance => _instance ??= _instanceFactory();
}
