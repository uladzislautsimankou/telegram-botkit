using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Telegram.BotKit.Commands.Invocation;

namespace Telegram.BotKit.Commands.Routing;

/// <summary>
/// Stores command triggers and aliases mapped to compiled registrations.
/// Registration happens once at application startup and is not thread-safe;
/// <see cref="Build"/> freezes the lookup tables, which are then safe for concurrent reads.
/// </summary>
internal sealed class CommandHandlerRegistry
{
    private readonly Dictionary<string, CommandRegistration> _handlerMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CommandRegistration> _aliasMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _commandKeys = new(StringComparer.OrdinalIgnoreCase);

    private FrozenDictionary<string, CommandRegistration> _frozenHandlerMap = FrozenDictionary<string, CommandRegistration>.Empty;
    private FrozenDictionary<string, CommandRegistration> _frozenAliasMap = FrozenDictionary<string, CommandRegistration>.Empty;

    /// <summary>
    /// Gets the searcher of similar command names used for spelling suggestions. Available after <see cref="Build"/>.
    /// </summary>
    public CommandSearcher Searcher { get; private set; } = null!;

    /// <summary>
    /// Registers a command with its aliases. Registering the same handler type again has no effect
    /// (a handler found by scanning may also be added manually); a different handler claiming a taken trigger or alias throws.
    /// </summary>
    /// <param name="command">The command trigger without the leading slash.</param>
    /// <param name="aliases">The alternative triggers of the command.</param>
    /// <param name="registration">The compiled registration of the handler.</param>
    /// <exception cref="InvalidOperationException">Thrown when an alias repeats the command or another alias of the handler, or a trigger or alias is already registered by a different handler.</exception>
    public void Register(string command, string[] aliases, CommandRegistration registration)
    {
        if (_handlerMap.TryGetValue(command, out var existing) && existing.HandlerType == registration.HandlerType)
        {
            return;
        }

        // the handler's own declaration is checked first, so that a mistake in it is reported as such
        // instead of as a clash with "another" handler, and nothing is registered half-way
        ValidateOwnAliases(command, aliases, registration.HandlerType);

        if (TryFindOwner(command, out var commandOwner))
        {
            throw new InvalidOperationException(
                $"Command trigger '/{command}' of '{registration.HandlerType.Name}' is already registered by '{commandOwner.HandlerType.Name}'.");
        }

        foreach (var alias in aliases)
        {
            if (TryFindOwner(alias, out var aliasOwner))
            {
                throw new InvalidOperationException(
                    $"Command alias '/{alias}' of '{registration.HandlerType.Name}' is already registered by '{aliasOwner.HandlerType.Name}'.");
            }
        }

        _handlerMap.Add(command, registration);
        _commandKeys.Add(command);

        foreach (var alias in aliases)
        {
            _aliasMap.Add(alias, registration);
        }
    }

    private static void ValidateOwnAliases(string command, string[] aliases, Type handlerType)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var alias in aliases)
        {
            if (alias.Equals(command, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Command alias '/{alias}' of '{handlerType.Name}' is the same as its own command '/{command}'. Remove it from Aliases.");
            }

            if (!seen.Add(alias))
            {
                throw new InvalidOperationException(
                    $"Command alias '/{alias}' is listed more than once in Aliases of '{handlerType.Name}'.");
            }
        }
    }

    private bool TryFindOwner(string trigger, [NotNullWhen(true)] out CommandRegistration? owner) =>
        _handlerMap.TryGetValue(trigger, out owner) || _aliasMap.TryGetValue(trigger, out owner);

    /// <summary>
    /// Gets the registrations of all commands (an alias shares the registration of its command).
    /// Available after <see cref="Build"/>.
    /// </summary>
    /// <returns>The registrations of all command handlers.</returns>
    public IEnumerable<CommandRegistration> GetRegistrations() => _frozenHandlerMap.Values;

    /// <summary>
    /// Freezes command registries into optimized immutable lookup tables.
    /// </summary>
    public void Build()
    {
        _frozenHandlerMap = _handlerMap.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _frozenAliasMap = _aliasMap.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        Searcher = new CommandSearcher(_commandKeys);

        // clear temporary dictionaries to free startup memory
        _handlerMap.Clear();
        _aliasMap.Clear();
    }

    /// <summary>
    /// Looks up the registration of a command trigger, ignoring case. Available after <see cref="Build"/>.
    /// </summary>
    /// <param name="key">The command trigger without the leading slash.</param>
    /// <param name="registration">The registration when found; otherwise, null.</param>
    /// <returns>True if a handler is registered under the trigger; otherwise, false.</returns>
    public bool TryGetRegistration(string key, [NotNullWhen(true)] out CommandRegistration? registration) =>
        _frozenHandlerMap.TryGetValue(key, out registration);

    /// <summary>
    /// Looks up the registration of a command alias, ignoring case. Available after <see cref="Build"/>.
    /// </summary>
    /// <param name="key">The alias without the leading slash.</param>
    /// <param name="registration">The registration when found; otherwise, null.</param>
    /// <returns>True if a handler is registered under the alias; otherwise, false.</returns>
    public bool TryGetAliasRegistration(string key, [NotNullWhen(true)] out CommandRegistration? registration) =>
        _frozenAliasMap.TryGetValue(key, out registration);
}


