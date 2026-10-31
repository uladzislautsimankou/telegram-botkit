using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Exceptions;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Intercepts exceptions thrown during command execution to reply with styled diagnostic HTML.
/// Registered before <c>UseRouting()</c>, so it wraps routing and execution.
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
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Custom Error Handler caught an exception");
            await bot.SendMessage(
                context.Message.Chat.Id,
                "Something went terribly wrong (Custom Handler).",
                cancellationToken: cancellationToken);
        }
    }
}
