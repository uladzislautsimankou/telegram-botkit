using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Telegram.BotKit.Hosting.Webhook;

/// <summary>
/// The HTTP endpoint that receives updates from Telegram in webhook mode.
/// </summary>
internal static class WebhookEndpoint
{
    private const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";

    /// <summary>
    /// Validates the request, reads the update and passes it to the <see cref="IUpdateHandler"/>.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="secretToken">The expected secret token, or null/empty when requests are not authenticated.</param>
    /// <param name="updateHandler">The application's update handler.</param>
    /// <param name="botClient">The client passed on to the update handler.</param>
    /// <param name="logger">The logger for problems with the request or the handler.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects or the host shuts down.</param>
    /// <returns>
    /// 401 for a wrong secret token, 400 for a body that is not an update, otherwise 200:
    /// an exception of the handler is logged and given to <see cref="IUpdateHandler.HandleErrorAsync"/>,
    /// it does not fail the request, because Telegram would redeliver the same update again and again.
    /// </returns>
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        string? secretToken,
        IUpdateHandler updateHandler,
        ITelegramBotClient botClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(secretToken))
        {
            if (!context.Request.Headers.TryGetValue(SecretTokenHeader, out var tokenHeader)
                || !IsSecretTokenValid(tokenHeader.ToString(), secretToken))
            {
                return Results.Unauthorized();
            }
        }

        // Telegram's own JSON settings (snake_case, its converters), not the ones of the ASP.NET application,
        // which the application's other endpoints rely on and which are not ours to change
        Update? update;
        try
        {
            update = await JsonSerializer.DeserializeAsync<Update>(context.Request.Body, JsonBotAPI.Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "The webhook request body is not a valid Telegram update");
            return Results.BadRequest();
        }

        if (update is null)
        {
            return Results.BadRequest();
        }

        try
        {
            await updateHandler.HandleUpdateAsync(botClient, update, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // the client went away or the host is stopping: nothing to answer, Telegram redelivers the update
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while processing update {UpdateId}", update.Id);
            await ReportErrorAsync(updateHandler, botClient, ex, logger, cancellationToken);
        }

        return Results.Ok();
    }

    // the same way polling reports exceptions of HandleUpdateAsync
    private static async Task ReportErrorAsync(
        IUpdateHandler updateHandler,
        ITelegramBotClient botClient,
        Exception exception,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await updateHandler.HandleErrorAsync(botClient, exception, HandleErrorSource.HandleUpdateError, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "IUpdateHandler.HandleErrorAsync failed");
        }
    }

    private static bool IsSecretTokenValid(string providedToken, string expectedToken)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
