using Microsoft.Extensions.Options;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Configuration;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Helpers;
using Telegram.BotKit.Invocation;
using Telegram.BotKit.Models;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class CommandRoutingMiddleware : ICommandMiddleware
{
    private readonly Dictionary<string, ICommandHandlerInvoker> _handlerMap;
    private readonly Dictionary<string, ICommandHandlerInvoker> _aliasMap;

    private readonly CommandSearcher _searcher;
    private readonly IBotInfo _botInfo;

    private readonly TelegramBotOptions _options;

    public CommandRoutingMiddleware(
        IEnumerable<ICommandHandlerInvoker> invokers,
        IBotInfo botInfo,
        IOptionsMonitor<TelegramBotOptions> optionsMonitor)
    {
        var commandKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _handlerMap = new Dictionary<string, ICommandHandlerInvoker>(StringComparer.OrdinalIgnoreCase);
        _aliasMap = new Dictionary<string, ICommandHandlerInvoker>(StringComparer.OrdinalIgnoreCase);

        _options = optionsMonitor.CurrentValue;

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
        var routeKey = context.VirtualRoute ?? context.Command;

        // если команда в игнор листе, то выходим
        if (_options.IgnoredCommands.Contains(context.Command, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.IsNullOrEmpty(context.TargetBotUsername)
            && !context.TargetBotUsername.Equals(_botInfo.Username, StringComparison.OrdinalIgnoreCase))
        {
            // команда вообще не для нашего бота
            return;
        }

        if (_handlerMap.TryGetValue(routeKey, out var invoker))
        {
            context.MatchedInvoker = invoker;
            context.MatchedCommandMetadata = new CommandMetadata(invoker.HandlerType, invoker.HandlerInstance);
            
            await next();
            return;
        }

        if (_aliasMap.TryGetValue(routeKey, out var invokerAlias))
        {
            // не нашли команду, но нашли алиас
            context.MatchedInvoker = invokerAlias;
            context.MatchedCommandMetadata = new CommandMetadata(invokerAlias.HandlerType, invokerAlias.HandlerInstance);

            await next();
            return;
        }

        // если роут виртуальный, значит его где-то присвоили, значит это команда для нас, но не найдена
        // значит тут мы должны выбросить ошибку
        if (context.VirtualRoute is not null)
        {
            throw new VirtualRouteNotFoundException(context.Command, context.VirtualRoute);
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
    }
}
