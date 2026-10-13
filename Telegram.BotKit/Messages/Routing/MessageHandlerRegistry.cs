using System.Collections.Frozen;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// A registry that groups message handlers by their required user states using frozen lookup structures.
/// </summary>
internal sealed class MessageHandlerRegistry
{
    private readonly Dictionary<string, List<MessageHandlerDescriptor>> _stateHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<MessageHandlerDescriptor> _globalHandlers = [];

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
            kvp => kvp.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        _frozenGlobalHandlers = _globalHandlers.ToArray();

        _stateHandlers.Clear();
        _globalHandlers.Clear();
    }

    public ReadOnlySpan<MessageHandlerDescriptor> GetHandlersForState(string state) =>
        _frozenStateHandlers.TryGetValue(state, out var handlers) ? handlers : [];

    public ReadOnlySpan<MessageHandlerDescriptor> GetGlobalHandlers() => _frozenGlobalHandlers;

    private FrozenDictionary<string, MessageHandlerDescriptor[]> _frozenStateHandlers = FrozenDictionary<string, MessageHandlerDescriptor[]>.Empty;
    private MessageHandlerDescriptor[] _frozenGlobalHandlers = [];
}