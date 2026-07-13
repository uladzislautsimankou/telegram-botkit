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
    private readonly List<Type> _registeredHandlerTypes = [];
    private bool _isInitialized;
    private readonly object _lock = new();

    public void RegisterHandlerType(Type handlerType) => _registeredHandlerTypes.Add(handlerType);

    public void Initialize(IServiceProvider serviceProvider)
    {
        if (_isInitialized)
            return;

        lock (_lock)
        {
            if (_isInitialized)
                return;

            using var scope = serviceProvider.CreateScope();
            foreach (var type in _registeredHandlerTypes)
            {
                var handler = (IMessageHandler)scope.ServiceProvider.GetRequiredService(type);

                if (handler.RequiredState is not null)
                {
                    if (!_stateHandlerTypes.TryGetValue(handler.RequiredState, out var list))
                    {
                        list = [];
                        _stateHandlerTypes[handler.RequiredState] = list;
                    }
                    list.Add(type);
                }
                else
                {
                    _globalHandlerTypes.Add(type);
                }
            }

            _isInitialized = true;
        }
    }

    public IReadOnlyList<Type> GetHandlerTypesForState(string state) => _stateHandlerTypes.TryGetValue(state, out var list)
        ? list
        : Array.Empty<Type>();

    public IReadOnlyList<Type> GetGlobalHandlerTypes() => _globalHandlerTypes;
}