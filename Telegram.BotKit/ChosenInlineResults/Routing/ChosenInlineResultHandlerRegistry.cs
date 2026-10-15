namespace Telegram.BotKit.ChosenInlineResults.Routing;

/// <summary>
/// A registry that stores chosen inline result handler descriptors.
/// </summary>
internal sealed class ChosenInlineResultHandlerRegistry
{
    private readonly List<ChosenInlineResultHandlerDescriptor> _handlers = [];
    private ChosenInlineResultHandlerDescriptor[] _frozenHandlers = [];

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
        _frozenHandlers = _handlers.ToArray();
        _handlers.Clear();
    }

    public ReadOnlySpan<ChosenInlineResultHandlerDescriptor> GetHandlers() => _frozenHandlers;
}
