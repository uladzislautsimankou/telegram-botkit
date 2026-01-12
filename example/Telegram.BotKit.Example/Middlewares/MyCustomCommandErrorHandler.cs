using Telegram.Bot;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Custom error handler that overrides the default behavior.
/// </summary>
public class MyCustomCommandErrorHandler(
    ITelegramBotClient bot,
    ILogger<MyCustomCommandErrorHandler> logger
    ) : ICommandErrorHandlerMiddleware
{
    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        catch (MissingParameterException ex)
        {
            // Custom response for missing parameters
            await bot.SendMessage(context.Message.Chat.Id,
                $"<b>Oops!</b> You forgot the parameter: <code>{ex.ParameterName}</code>",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Custom Error Handler caught an exception");
            await bot.SendMessage(context.Message.Chat.Id, "Something went terribly wrong (Custom Handler).", cancellationToken: cancellationToken);
        }
    }
}