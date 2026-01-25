using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Helpers;
using Telegram.BotKit.Invocation;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class CommandRoutingMiddleware : ICommandMiddleware
{
    private readonly Dictionary<string, ICommandHandlerInvoker> _handlerMap;
    private readonly Dictionary<string, ICommandHandlerInvoker> _aliasMap;

    private readonly CommandSearcher _searcher;
    private readonly IBotInfo _botInfo;

    public CommandRoutingMiddleware(IEnumerable<ICommandHandlerInvoker> invokers, IBotInfo botInfo)
    {
        var commandKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _handlerMap = new Dictionary<string, ICommandHandlerInvoker>(StringComparer.OrdinalIgnoreCase);
        _aliasMap = new Dictionary<string, ICommandHandlerInvoker>(StringComparer.OrdinalIgnoreCase);

        foreach (var invoker in invokers)
        {
            commandKeys.Add(invoker.Command);
            _handlerMap.TryAdd(invoker.Command, invoker);

            foreach (var alias in invoker.Aliases)
                _aliasMap.TryAdd(alias, invoker);
        }

        _searcher = new CommandSearcher(commandKeys);
        _botInfo = botInfo;
    }

    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(context.TargetBotUsername)
            && !context.TargetBotUsername.Equals(_botInfo.Username, StringComparison.OrdinalIgnoreCase))
        {
            // комманда вообще не для нашего бота
            return;
        }

        if (_handlerMap.TryGetValue(context.Command, out var invoker))
        {
            // все ок, нашли команду
            await invoker.InvokeAsync(context, cancellationToken);
            return;
        }

        if (_aliasMap.TryGetValue(context.Command, out var invokerAlias))
        {
            // не нашли команду, но нашли алиас
            await invokerAlias.InvokeAsync(context, cancellationToken);
            return;
        }

        // ничего не нашли - ищем похожие команды
        var similar = _searcher.FindSimilar(context.Command);

        if (similar.Count > 0)
        {
            throw new CommandSuggestionsException(context.Command, similar);
        }

        // в приватных выбрасываем обычную ошибку
        if (context.Message.Chat.Type == ChatType.Private)
        {
            throw new RouteNotFoundException(context.Command);
        }

        // в остальных чатах игнорируем, вероятно не нам команда
        return;

        // next() не вызываем, это конец пути
    }
}
