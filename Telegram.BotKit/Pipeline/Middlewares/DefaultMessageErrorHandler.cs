using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Extensions;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class DefaultMessageErrorHandler(
    ITelegramBotClient bot,
    ILogger<DefaultMessageErrorHandler> logger
    ) : IMessageErrorHandlerMiddleware
{
    public async Task InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error handling message {MessageType}", context.Message.Type);

            await TrySendMessageAsync(context,
                "🔥 An internal error occurred while processing your command.",
                cancellationToken);
        }
    }

    private async Task TrySendMessageAsync(MessageContext context, string text, CancellationToken ct)
    {
        try
        {
            await bot.ReplyMessage(context, text, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // log the failure if the error response cannot be sent for some reason
            logger.LogWarning(ex, "Failed to send error response to chat {ChatId}", context.Message.Chat.Id);
        }
    }
}