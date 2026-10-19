namespace Telegram.BotKit.ChosenInlineResults.Routing;

/// <summary>
/// A registry that stores chosen inline result handler descriptors.
/// </summary>
internal sealed class ChosenInlineResultHandlerRegistry
{
    private readonly List<ChosenInlineResultHandlerDescriptor> _handlers = [];
    private ChosenInlineResultHandlerDescriptor[] _frozenHandlers = [];

    /// <summary>
    /// Registers a handler descriptor. Registering the same handler type again has no effect.
    /// </summary>
    /// <param name="descriptor">The descriptor of the handler.</param>
    public void Register(ChosenInlineResultHandlerDescriptor descriptor)
    {
        if (!_handlers.Any(d => d.HandlerType == descriptor.HandlerType))
        {
            _handlers.Add(descriptor);
        }
    }

    /// <summary>
    /// Builds immutable lookup structures and flattens handler arrays.
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
    public ReadOnlySpan<ChosenInlineResultHandlerDescriptor> GetHandlers() => _frozenHandlers;
}
