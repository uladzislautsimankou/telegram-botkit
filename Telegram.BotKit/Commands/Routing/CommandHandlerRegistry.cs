using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Commands.Invocation;

namespace Telegram.BotKit.Commands.Routing;

/// <summary>
/// A thread-safe registry that stores command triggers and aliases mapped to their invoker types.
/// Initializes search and routing data lazily on the first request.
/// </summary>
internal sealed class CommandHandlerRegistry
{
    private readonly Dictionary<string, Type> _handlerMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Type> _aliasMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _commandKeys = new(StringComparer.OrdinalIgnoreCase);

    public CommandSearcher Searcher { get; private set; } = null!;

    public void Register(string command, string[] aliases, Type invokerType)
    {
        _commandKeys.Add(command);
        _handlerMap[command] = invokerType;

        foreach (var alias in aliases)
        {
            _aliasMap[alias] = invokerType;
        }
    }

    public void Build() => Searcher = new CommandSearcher(_commandKeys);

    public bool TryGetInvokerType(string key, out Type? invokerType) => _handlerMap.TryGetValue(key, out invokerType);

    public bool TryGetAliasInvokerType(string key, out Type? invokerType) => _aliasMap.TryGetValue(key, out invokerType);
}