using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Commands.Routing;

/// <summary>
/// Routing middleware responsible for identifying the correct handler based on the command context and bot state.
/// </summary>
internal sealed class CommandRoutingMiddleware(
    CommandHandlerRegistry registry,
    IBotInfo botInfo,
    IServiceProvider serviceProvider) : ICommandMiddleware
{
    public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        var routeKey = context.VirtualRoute ?? context.Command;

        if (!string.IsNullOrEmpty(context.TargetBotUsername) &&
            !context.TargetBotUsername.Equals(botInfo.Username, StringComparison.OrdinalIgnoreCase))
        {
            context.RouteResult = CommandRouteResult.Ignored();
            return ValueTask.CompletedTask;
        }

        if (registry.TryGetRegistration(routeKey, out var registration) ||
            registry.TryGetAliasRegistration(routeKey, out registration))
        {
            var handlerInstance = registration!.Factory(serviceProvider);
            context.MatchedRegistration = registration;
            context.MatchedCommandMetadata = new CommandMetadata(registration.HandlerType, handlerInstance);
            context.RouteResult = CommandRouteResult.Success();

            return next();
        }

        if (context.VirtualRoute is not null)
        {
            context.RouteResult = CommandRouteResult.VirtualRouteNotFound();
            return ValueTask.CompletedTask;
        }

        var similar = registry.Searcher.FindSimilar(context.Command);
        if (similar.Count > 0)
        {
            context.RouteResult = CommandRouteResult.SuggestionsFound(similar);
            return ValueTask.CompletedTask;
        }

        var isExplicitlyForUs = context.Message.Chat.Type == ChatType.Private
            || !string.IsNullOrEmpty(context.TargetBotUsername);

        context.RouteResult = isExplicitlyForUs
            ? CommandRouteResult.NotFound()
            : CommandRouteResult.Ignored();

        return ValueTask.CompletedTask;
    }
}
