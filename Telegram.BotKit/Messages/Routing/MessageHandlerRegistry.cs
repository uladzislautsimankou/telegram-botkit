using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Messages.Abstractions;

namespace Telegram.BotKit.Messages.Routing;

/// <summary>
/// A thread-safe registry that groups message handler types by their required user states.
/// Resolves metadata lazily on the first message routing request.
/// </summary>
internal sealed class MessageHandlerRegistry
{
    private readonly Dictionary<string, List<Type>> _stateHandlerTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Type> _globalHandlerTypes = [];

    public void Register(Type handlerType, string? requiredState)
    {
        if (requiredState is not null)
        {
            if (!_stateHandlerTypes.TryGetValue(requiredState, out var list))
            {
                list = [];
                _stateHandlerTypes[requiredState] = list;
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
        : Array.Empty<Type>();

    public IReadOnlyList<Type> GetGlobalHandlerTypes() => _globalHandlerTypes;
}