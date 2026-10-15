using System.Collections.Frozen;
using Telegram.BotKit.Commands.Invocation;

namespace Telegram.BotKit.Commands.Routing;

/// <summary>
/// A thread-safe registry that stores command triggers and aliases mapped to compiled registrations.
/// </summary>
internal sealed class CommandHandlerRegistry
{
    private readonly Dictionary<string, CommandRegistration> _handlerMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CommandRegistration> _aliasMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _commandKeys = new(StringComparer.OrdinalIgnoreCase);

    private FrozenDictionary<string, CommandRegistration> _frozenHandlerMap = FrozenDictionary<string, CommandRegistration>.Empty;
    private FrozenDictionary<string, CommandRegistration> _frozenAliasMap = FrozenDictionary<string, CommandRegistration>.Empty;

    public CommandSearcher Searcher { get; private set; } = null!;

    public void Register(string command, string[] aliases, CommandRegistration registration)
    {
        if (_aliasMap.ContainsKey(command) || !_handlerMap.TryAdd(command, registration))
        {
            throw new InvalidOperationException($"Command trigger '/{command}' is already registered.");
        }

        _commandKeys.Add(command);

        for (var i = 0; i < aliases.Length; i++)
        {
            var alias = aliases[i];
            if (_handlerMap.ContainsKey(alias) || !_aliasMap.TryAdd(alias, registration))
            {
                throw new InvalidOperationException($"Command alias '/{alias}' is already registered.");
            }
        }
    }

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

    public bool TryGetRegistration(string key, out CommandRegistration? registration) =>
        _frozenHandlerMap.TryGetValue(key, out registration);

    public bool TryGetAliasRegistration(string key, out CommandRegistration? registration) =>
        _frozenAliasMap.TryGetValue(key, out registration);
}
