using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Messages.Abstractions;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// A thread-safe registry that groups message handler types by their required user states.
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
            list.Add(handlerType);
        }
        else
        {
            _globalHandlerTypes.Add(handlerType);
        }
    }

    public IReadOnlyList<Type> GetHandlerTypesForState(string state) => _stateHandlerTypes.TryGetValue(state, out var list)
        ? list
        : [];

    public IReadOnlyList<Type> GetGlobalHandlerTypes() => _globalHandlerTypes;
}