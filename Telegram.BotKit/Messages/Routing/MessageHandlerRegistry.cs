using System.Collections.Frozen;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// A registry that groups message handler types by their required user states using frozen lookup structures.
/// </summary>
internal sealed class MessageHandlerRegistry
{
    private readonly Dictionary<string, List<Type>> _stateHandlerTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Type> _globalHandlerTypes = [];

    public void Register(Type handlerType, string? requiredState)
    {
        if (!string.IsNullOrWhiteSpace(requiredState))
        {
            var trimmedState = requiredState.Trim();
            if (!_stateHandlerTypes.TryGetValue(trimmedState, out var list))
            {
                list = [];
                _stateHandlerTypes[trimmedState] = list;
            }

            if (!list.Contains(handlerType))
            {
                list.Add(handlerType);
            }
        }
        else
        {
            if (!_globalHandlerTypes.Contains(handlerType))
            {
                _globalHandlerTypes.Add(handlerType);
            }
        }
    }

    /// <summary>
    /// Builds immutable frozen state lookup structures and flattens handler type arrays.
    /// </summary>
    public void Build()
    {
        _frozenStateHandlerTypes = _stateHandlerTypes.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        _frozenGlobalHandlerTypes = _globalHandlerTypes.ToArray();

        // clear mutable builders to release startup memory
        _stateHandlerTypes.Clear();
        _globalHandlerTypes.Clear();
    }

    public ReadOnlySpan<Type> GetHandlerTypesForState(string state) =>
        _frozenStateHandlerTypes.TryGetValue(state, out var types) ? types : [];

    public ReadOnlySpan<Type> GetGlobalHandlerTypes() => _frozenGlobalHandlerTypes;

    private FrozenDictionary<string, Type[]> _frozenStateHandlerTypes = FrozenDictionary<string, Type[]>.Empty;
    private Type[] _frozenGlobalHandlerTypes = [];
}