using System.Collections.Frozen;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// A registry that groups message handlers by their required user states using frozen lookup structures.
/// </summary>
internal sealed class MessageHandlerRegistry
{
    private readonly Dictionary<string, List<MessageHandlerDescriptor>> _stateHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<MessageHandlerDescriptor> _globalHandlers = [];

    /// <summary>
    /// Registers a handler descriptor under its required user state, or as a global handler when there is none.
    /// Registering the same handler type again for the same state has no effect.
    /// </summary>
    /// <param name="descriptor">The descriptor of the handler.</param>
    /// <param name="requiredState">The user state the handler requires; null or whitespace for a global handler.</param>
    public void Register(MessageHandlerDescriptor descriptor, string? requiredState)
    {
        if (!string.IsNullOrWhiteSpace(requiredState))
        {
            var trimmedState = requiredState.Trim();
            if (!_stateHandlers.TryGetValue(trimmedState, out var list))
            {
                list = [];
                _stateHandlers[trimmedState] = list;
            }

            if (!list.Any(d => d.HandlerType == descriptor.HandlerType))
            {
                list.Add(descriptor);
            }
        }
        else
        {
            if (!_globalHandlers.Any(d => d.HandlerType == descriptor.HandlerType))
            {
                _globalHandlers.Add(descriptor);
            }
        }
    }

    /// <summary>
    /// Builds immutable frozen state lookup structures and flattens handler arrays.
    /// </summary>
    public void Build()
    {
        _frozenStateHandlers = _stateHandlers.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.OrderBy(d => d.Order).ToArray(),
            StringComparer.OrdinalIgnoreCase);

        _frozenGlobalHandlers = _globalHandlers.OrderBy(d => d.Order).ToArray();

        _stateHandlers.Clear();
        _globalHandlers.Clear();
    }

    /// <summary>
    /// Gets the handlers bound to a user state, ordered by priority. Available after <see cref="Build"/>.
    /// </summary>
    /// <param name="state">The user state, compared ignoring case.</param>
    /// <returns>The handler descriptors of the state in evaluation order; empty when the state has none.</returns>
    public ReadOnlySpan<MessageHandlerDescriptor> GetHandlersForState(string state) =>
        _frozenStateHandlers.TryGetValue(state, out var handlers) ? handlers : [];

    /// <summary>
    /// Gets the state-agnostic handlers, ordered by priority. Available after <see cref="Build"/>.
    /// </summary>
    /// <returns>The global handler descriptors in evaluation order.</returns>
    public ReadOnlySpan<MessageHandlerDescriptor> GetGlobalHandlers() => _frozenGlobalHandlers;

    private FrozenDictionary<string, MessageHandlerDescriptor[]> _frozenStateHandlers = FrozenDictionary<string, MessageHandlerDescriptor[]>.Empty;
    private MessageHandlerDescriptor[] _frozenGlobalHandlers = [];
}
