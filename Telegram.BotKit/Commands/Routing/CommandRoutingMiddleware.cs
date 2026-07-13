using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Commands.Routing;

/// <summary>
/// Routing middleware responsible for identifying the correct handler based on the command context and bot state.
/// </summary>
internal sealed class CommandRoutingMiddleware(
    CommandHandlerRegistry registry,
    IBotInfo botInfo,
    IOptionsMonitor<TelegramBotOptions> optionsMonitor,
    IServiceProvider serviceProvider
    ) : ICommandMiddleware
{
    private readonly TelegramBotOptions _options = optionsMonitor.CurrentValue;

    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        registry.Initialize(serviceProvider);

        var routeKey = context.VirtualRoute ?? context.Command;

        // exit early if the command is in the ignore list
        if (_options.IgnoredCommands.Contains(context.Command, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.IsNullOrEmpty(context.TargetBotUsername) && 
            !context.TargetBotUsername.Equals(botInfo.Username, StringComparison.OrdinalIgnoreCase))
        {
            // the command is not targeted at our bot
            return;
        }

        if (registry.TryGetInvokerType(routeKey, out var invokerType))
        {
            var invoker = (ICommandHandlerInvoker)serviceProvider.GetRequiredService(invokerType!);
            context.MatchedInvoker = invoker;
            context.MatchedCommandMetadata = new CommandMetadata(invoker.HandlerType, invoker.HandlerInstance);

            await next();
            return;
        }

        if (registry.TryGetAliasInvokerType(routeKey, out var aliasInvokerType))
        {
            var invokerAlias = (ICommandHandlerInvoker)serviceProvider.GetRequiredService(aliasInvokerType!);
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
        var similar = registry.Searcher.FindSimilar(context.Command);

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