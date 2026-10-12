using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Intercepts exceptions thrown during command execution to reply with styled diagnostic HTML.
/// </summary>
internal sealed class MyCustomCommandErrorHandler(
    ITelegramBotClient bot,
    ILogger<MyCustomCommandErrorHandler> logger
    ) : ICommandMiddleware
{
    public async ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        catch (MissingParameterException ex)
        {
            // custom HTML notification for missing fields
            await bot.SendMessage(context.Message.Chat.Id,
                $"<b>Oops!</b> You forgot the parameter: <code>{ex.ParameterName}</code>",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Custom Error Handler caught an exception");
            await bot.SendMessage(
                context.Message.Chat.Id,
                "Something went terribly wrong (Custom Handler).",
                cancellationToken: cancellationToken);

            return;
        }

        // handle routing results
        switch (context.RouteResult.Status)
        {
            case CommandRouteStatus.Suggestions:
                logger.LogDebug(
                    "Unknown command with suggestions: /{Command}, suggestions: {Suggestions}",
                    context.Command,
                    string.Join(", ", context.RouteResult.Suggestions));

                var suggestions = string.Join(", ", context.RouteResult.Suggestions.Select(c => $"<code>/{c}</code>"));

                await bot.SendMessage(
                    chatId: context.Message.Chat.Id,
                    text: $"🤷 Unknown command <code>/{context.Command}</code>.\nDid you mean: {suggestions}?",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                break;

            case CommandRouteStatus.NotFound:
                await bot.SendMessage(
                    chatId: context.Message.Chat.Id,
                    text: $"🤷 Command <code>/{context.Command}</code> not found.",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                break;

            case CommandRouteStatus.VirtualRouteNotFound:
                logger.LogWarning("Virtual route '{VirtualRoute}' was not found for command '{Command}'", context.VirtualRoute, context.Command);
                break;
        }
    }
}