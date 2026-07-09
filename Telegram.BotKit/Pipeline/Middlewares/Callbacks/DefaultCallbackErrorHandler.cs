using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Pipeline.Middlewares;

/// <summary>
/// A default implementation of <see cref="ICallbackErrorHandlerMiddleware"/> that provides
/// global error handling during callback query execution, displaying alerts for validation failures or internal errors.
/// </summary>
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
        // differentiate between framework-specific validation/execution errors and unexpected internal exceptions
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
        catch (Exception ex)
        {
            // log a warning since callback query answers frequently fail if the query expires (e.g., query too old)
            logger.LogWarning(ex, "Failed to send callback error response for query '{QueryId}'. The query may have expired.", context.Query.Id);
        }
    }
}
