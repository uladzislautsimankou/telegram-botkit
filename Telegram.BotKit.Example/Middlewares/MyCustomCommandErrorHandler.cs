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
    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
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
        }
        catch (CommandSuggestionsException ex)
        {
            logger.LogDebug("Unknown command with suggestions: /{Command}, suggestions: {Suggestions}",
                ex.Command, string.Join(", ", ex.Suggestions));

            var suggestions = string.Join(", ", ex.Suggestions.Select(c => $"<code>/{c}</code>"));
            await bot.SendMessage(context.Message.Chat.Id,
                $"🤷‍ Unknown command <code>/{ex.Command}</code>.\nDid you mean: {suggestions}?",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Custom Error Handler caught an exception");
            await bot.SendMessage(context.Message.Chat.Id, "Something went terribly wrong (Custom Handler).", cancellationToken: cancellationToken);
        }
    }
}