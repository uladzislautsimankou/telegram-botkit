using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Placed after <c>UseRouting()</c>: reacts to the result of command routing.
/// It runs for unmatched commands too, so it is the place to answer "unknown command" and typo suggestions.
/// </summary>
internal sealed class CommandRoutingResultMiddleware(
    ITelegramBotClient bot,
    ILogger<CommandRoutingResultMiddleware> logger
    ) : ICommandMiddleware
{
    public async ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        var chatId = context.Message.Chat.Id;

        switch (context.RouteResult.Status)
        {
            // routing succeeded: continue to the handler
            case CommandRouteStatus.Success:
                logger.LogDebug("Command '/{Command}' is routed to {Handler}",
                    context.Command, context.MatchedCommandMetadata?.HandlerType.Name);
                await next();
                break;

            // routing failed: answer the user and stop
            case CommandRouteStatus.Suggestions:
                var suggestions = string.Join(", ", context.RouteResult.Suggestions.Select(c => $"<code>/{c}</code>"));
                await bot.SendMessage(
                    chatId: chatId,
                    text: $"🤷 Unknown command <code>/{context.Command}</code>.\nDid you mean: {suggestions}?",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                break;

            case CommandRouteStatus.NotFound:
                await bot.SendMessage(
                    chatId: chatId,
                    text: $"🤷 Command <code>/{context.Command}</code> not found.",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                break;

            case CommandRouteStatus.VirtualRouteNotFound:
                logger.LogWarning("Virtual route '{VirtualRoute}' was not found for command '{Command}'",
                    context.VirtualRoute, context.Command);
                break;

            // addressed to another bot or not meant for us: stay silent
            case CommandRouteStatus.Ignored:
                logger.LogDebug("Command '/{Command}' is ignored", context.Command);
                break;
        }
    }
}
