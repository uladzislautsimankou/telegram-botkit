namespace Telegram.BotKit.InlineQueries.Routing;

/// <summary>
/// A registry that stores inline query handler descriptors.
/// </summary>
internal sealed class InlineQueryHandlerRegistry
{
    private readonly List<InlineQueryHandlerDescriptor> _handlers = [];
    private InlineQueryHandlerDescriptor[] _frozenHandlers = [];

    /// <summary>
    /// Registers a handler descriptor. Registering the same handler type again has no effect.
    /// </summary>
    /// <param name="descriptor">The descriptor of the handler.</param>
    public void Register(InlineQueryHandlerDescriptor descriptor)
    {
        if (!_handlers.Any(d => d.HandlerType == descriptor.HandlerType))
        {
            _handlers.Add(descriptor);
        }
    }

    /// <summary>
    /// Flattens registered descriptors into an immutable array.
    /// </summary>
    public void Build()
    {
        _frozenHandlers = _handlers.OrderBy(d => d.Order).ToArray();
        _handlers.Clear();
    }

    /// <summary>
    /// Gets the registered handlers ordered by priority. Available after <see cref="Build"/>.
    /// </summary>
    /// <returns>The handler descriptors in evaluation order.</returns>
    public ReadOnlySpan<InlineQueryHandlerDescriptor> GetHandlers() => _frozenHandlers;
}
