using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Commands.Routing;

/// <summary>
/// Routing middleware responsible for identifying the correct handler based on the command context and bot state.
/// </summary>
/// <param name="registry">The registry of command handlers.</param>
/// <param name="botInfo">The identity of the running bot, used to ignore commands addressed to another bot.</param>
/// <param name="serviceProvider">The service provider used to create the matched handler.</param>
internal sealed class CommandRoutingMiddleware(
    CommandHandlerRegistry registry,
    IBotInfo botInfo,
    IServiceProvider serviceProvider) : ICommandMiddleware
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        context.RouteResult = Route(context);

        // always continue: later middleware may react to NotFound/Suggestions/Ignored,
        // the execution middleware skips contexts without a matched handler
        return next();
    }

    private CommandRouteResult Route(CommandContext context)
    {
        if (!string.IsNullOrEmpty(context.TargetBotUsername) &&
            !context.TargetBotUsername.Equals(botInfo.Username, StringComparison.OrdinalIgnoreCase))
        {
            return CommandRouteResult.Ignored();
        }

        var routeKey = context.VirtualRoute ?? context.Command;

        if (registry.TryGetRegistration(routeKey, out var registration) ||
            registry.TryGetAliasRegistration(routeKey, out registration))
        {
            context.MatchedRegistration = registration;

            // the handler is created lazily by the execution middleware
            context.MatchedCommandMetadata = new CommandMetadata(
                registration.HandlerType,
                () => registration.Factory(serviceProvider));

            return CommandRouteResult.Success();
        }

        if (context.VirtualRoute is not null)
        {
            return CommandRouteResult.VirtualRouteNotFound();
        }

        var similar = registry.Searcher.FindSimilar(context.Command);
        if (similar.Count > 0)
        {
            return CommandRouteResult.SuggestionsFound(similar);
        }

        var isExplicitlyForUs = context.Message.Chat.Type == ChatType.Private
            || !string.IsNullOrEmpty(context.TargetBotUsername);

        return isExplicitlyForUs
            ? CommandRouteResult.NotFound()
            : CommandRouteResult.Ignored();
    }
}
