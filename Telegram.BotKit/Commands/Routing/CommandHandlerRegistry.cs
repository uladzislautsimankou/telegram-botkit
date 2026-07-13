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
    private readonly List<Type> _invokerTypes = [];
    private CommandSearcher? _searcher;
    private bool _isInitialized;
    private readonly object _lock = new();

    public CommandSearcher Searcher => _searcher ?? throw new InvalidOperationException("Registry is not initialized yet.");

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
                var invoker = (ICommandHandlerInvoker)scope.ServiceProvider.GetRequiredService(type);

                _commandKeys.Add(invoker.Command);
                _handlerMap[invoker.Command] = type;

                foreach (var alias in invoker.Aliases)
                {
                    _aliasMap[alias] = type;
                }
            }

            _searcher = new CommandSearcher(_commandKeys);
            _isInitialized = true;
        }
    }

    public bool TryGetInvokerType(string key, out Type? invokerType) => _handlerMap.TryGetValue(key, out invokerType);

    public bool TryGetAliasInvokerType(string key, out Type? invokerType) => _aliasMap.TryGetValue(key, out invokerType);
}