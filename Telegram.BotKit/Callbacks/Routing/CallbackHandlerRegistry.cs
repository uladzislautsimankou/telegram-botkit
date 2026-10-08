namespace Telegram.BotKit.Callbacks.Routing;

/// <summary>
/// Registers and maintains mapping of unique callback keys to their corresponding invoker types.
/// </summary>
internal sealed class CallbackHandlerRegistry
{
    private readonly Dictionary<string, Type> _registry = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string key, Type invokerType) => _registry[key] = invokerType;

    public bool TryGetInvokerType(string key, out Type? invokerType) => _registry.TryGetValue(key, out invokerType);
}