using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Callbacks.Invocation;

namespace Telegram.BotKit.Callbacks.Routing;

/// <summary>
/// Registers and maintains mapping of unique callback keys to their corresponding invoker types.
/// </summary>
internal sealed class CallbackHandlerRegistry
{
    private readonly Dictionary<string, Type> _registry = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Type> _invokerTypes = [];
    private bool _isInitialized;
    private readonly object _lock = new();

    public void RegisterInvokerType(Type invokerType) => _invokerTypes.Add(invokerType);

    public void Initialize(IServiceProvider serviceProvider)
    {
        if (_isInitialized)
            return;

        lock (_lock)
        {
            if (_isInitialized)
                return;

            using var scope = serviceProvider.CreateScope();
            foreach (var type in _invokerTypes)
            {
                var invoker = (ICallbackHandlerInvoker)scope.ServiceProvider.GetRequiredService(type);
                _registry[invoker.Key] = type;
            }

            _isInitialized = true;
        }
    }

    public bool TryGetInvokerType(string key, out Type? invokerType) => _registry.TryGetValue(key, out invokerType);
}