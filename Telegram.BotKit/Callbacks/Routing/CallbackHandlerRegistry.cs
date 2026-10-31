using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Telegram.BotKit.Callbacks.Invocation;

namespace Telegram.BotKit.Callbacks.Routing;

/// <summary>
/// Stores callback route keys mapped to compiled registrations.
/// Registration happens once at application startup and is not thread-safe;
/// <see cref="Build"/> freezes the lookup table, which is then safe for concurrent reads.
/// </summary>
internal sealed class CallbackHandlerRegistry
{
    private readonly Dictionary<string, CallbackRegistration> _registry = new(StringComparer.OrdinalIgnoreCase);
    private FrozenDictionary<string, CallbackRegistration> _frozenRegistry = FrozenDictionary<string, CallbackRegistration>.Empty;

    /// <summary>
    /// Registers a handler under its route key. Registering the same handler type again has no effect
    /// (a handler found by scanning may also be added manually); a different handler claiming a taken key throws.
    /// </summary>
    /// <param name="key">The route key.</param>
    /// <param name="registration">The compiled registration of the handler.</param>
    /// <exception cref="InvalidOperationException">Thrown when the key is already registered by a different handler.</exception>
    public void Register(string key, CallbackRegistration registration)
    {
        if (_registry.TryGetValue(key, out var existing) && existing.HandlerType == registration.HandlerType)
        {
            return;
        }

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

    /// <summary>
    /// Gets all registrations. Available after <see cref="Build"/>.
    /// </summary>
    /// <returns>The registrations of all callback handlers.</returns>
    public IEnumerable<CallbackRegistration> GetRegistrations() => _frozenRegistry.Values;

    /// <summary>
    /// Looks up the registration of a route key, ignoring case. Available after <see cref="Build"/>.
    /// </summary>
    /// <param name="key">The route key.</param>
    /// <param name="registration">The registration when found; otherwise, null.</param>
    /// <returns>True if a handler is registered under the key; otherwise, false.</returns>
    public bool TryGetRegistration(string key, [NotNullWhen(true)] out CallbackRegistration? registration) =>
        _frozenRegistry.TryGetValue(key, out registration);
}


