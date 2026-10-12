using System.Collections.Frozen;
using Telegram.BotKit.Callbacks.Invocation;

namespace Telegram.BotKit.Callbacks.Routing;

/// <summary>
/// A thread-safe registry that stores callback keys mapped to compiled registrations.
/// </summary>
internal sealed class CallbackHandlerRegistry
{
    private readonly Dictionary<string, CallbackRegistration> _registry = new(StringComparer.OrdinalIgnoreCase);
    private FrozenDictionary<string, CallbackRegistration> _frozenRegistry = FrozenDictionary<string, CallbackRegistration>.Empty;

    public void Register(string key, CallbackRegistration registration)
    {
        if (!_registry.TryAdd(key, registration))
        {
            throw new InvalidOperationException($"Callback route key '{key}' is already registered.");
        }
    }

    /// <summary>
    /// Freezes callback registrations into an optimized immutable lookup table.
    /// </summary>
    public void Build()
    {
        _frozenRegistry = _registry.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _registry.Clear();
    }

    public bool TryGetRegistration(string key, out CallbackRegistration? registration) =>
        _frozenRegistry.TryGetValue(key, out registration);
}