using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal class DefaultCallbackErrorHandler(
    ITelegramBotClient bot,
    ILogger<DefaultCallbackErrorHandler> logger
    ) : ICallbackErrorHandlerMiddleware
{
    public async Task InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        // в целом, тут если и есть ошибки, то это не пользователь виноват, так что просто разделим на "наши", и "не наши"
        catch (BotKitException ex)
        {
            logger.LogWarning(ex, "Callback validation failed for key '{Key}'. Params: {Params}",
                context.Key, string.Join(", ", context.RawParams));

            await TryAnswerAsync(context, $"⚠️ Error: {ex.Message}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error handling callback '{Key}'", context.Key);

            await TryAnswerAsync(context, "🔥 An internal error occurred.", cancellationToken);
        }
    }

    private async Task TryAnswerAsync(CallbackContext context, string text, CancellationToken ct)
    {
        try
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: context.Query.Id,
                text: text,
                showAlert: true,
                cancellationToken: ct);
        }
        catch
        {
            // не ответили и не ответили) может просто "query too old"
        }
    }
}
