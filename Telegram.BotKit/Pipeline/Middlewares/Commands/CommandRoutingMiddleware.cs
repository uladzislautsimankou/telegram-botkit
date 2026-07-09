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

        // exit early if the command is in the ignore list
        if (_options.IgnoredCommands.Contains(context.Command, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.IsNullOrEmpty(context.TargetBotUsername)
            && !context.TargetBotUsername.Equals(_botInfo.Username, StringComparison.OrdinalIgnoreCase))
        {
            // the command is not targeted at our bot
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
            // command was not found, but a matching alias was found
            context.MatchedInvoker = invokerAlias;
            context.MatchedCommandMetadata = new CommandMetadata(invokerAlias.HandlerType, invokerAlias.HandlerInstance);

            await next();
            return;
        }

        // if the route is virtual, it was explicitly assigned elsewhere, meaning it targets our bot but was not found; throw an exception
        if (context.VirtualRoute is not null)
        {
            throw new VirtualRouteNotFoundException(context.Command, context.VirtualRoute);
        }

        // no exact match found, search for similar command suggestions
        var similar = _searcher.FindSimilar(context.Command);

        if (similar.Count > 0)
        {
            throw new CommandSuggestionsException(context.Command, similar);
        }

        // throw a standard exception for private chats
        if (context.Message.Chat.Type == ChatType.Private)
        {
            throw new RouteNotFoundException(context.Command);
        }

        // ignore in other chat types, as the command is likely intended for another bot
        return;
    }
}