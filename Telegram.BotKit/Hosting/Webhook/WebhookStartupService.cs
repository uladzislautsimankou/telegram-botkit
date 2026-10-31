using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Hosting.Webhook;

/// <summary>
/// Hosted service that identifies the bot and registers the webhook with Telegram on start, and optionally removes it on stop.
/// </summary>
/// <param name="bot">The Telegram bot client.</param>
/// <param name="botInfo">The identity holder filled in when the bot connects.</param>
/// <param name="options">The bot options.</param>
/// <param name="logger">The logger of the service.</param>
internal sealed class WebhookStartupService(
    ITelegramBotClient bot,
    BotInfo botInfo,
    IOptions<TelegramBotOptions> options,
    ILogger<WebhookStartupService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var botOptions = options.Value;

        if (string.IsNullOrEmpty(botOptions.SecretToken))
        {
            logger.LogWarning(
                "TelegramBot:SecretToken is not set. Anyone who knows the webhook URL can send forged updates to the bot.");
        }

        try
        {
            await bot.InitializeIdentityAsync(botInfo, logger, cancellationToken);

            logger.LogInformation("Setting up Webhook: {Url}", botOptions.WebhookUrl);

            InputFileStream? certificateInput = null;
            FileStream? certStream = null;

            if (!string.IsNullOrWhiteSpace(botOptions.CertificatePath))
            {
                if (File.Exists(botOptions.CertificatePath))
                {
                    certStream = File.OpenRead(botOptions.CertificatePath);
                    certificateInput = InputFile.FromStream(certStream, Path.GetFileName(botOptions.CertificatePath));
                    logger.LogInformation("Using self-signed certificate: {Path}", botOptions.CertificatePath);
                }
                else
                {
                    logger.LogWarning("Certificate file not found at: {Path}. Webhook will be set without certificate.", botOptions.CertificatePath);
                }
            }

            try
            {
                await bot.SetWebhook(
                    url: botOptions.WebhookUrl,
                    certificate: certificateInput,
                    ipAddress: botOptions.IpAddress,
                    maxConnections: botOptions.MaxConnections,
                    allowedUpdates: botOptions.ParsedAllowedUpdates,
                    dropPendingUpdates: botOptions.DropPendingUpdates,
                    secretToken: botOptions.SecretToken,
                    cancellationToken: cancellationToken);

                logger.LogInformation("Webhook registered successfully");
            }
            finally
            {
                if (certStream is not null)
                {
                    await certStream.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to start Webhook mode.");
            throw new BotStartupException("Failed to initialize bot in Webhook mode.", ex);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.DeleteWebhookOnShutdown)
        {
            return;
        }

        logger.LogInformation("Removing Webhook...");

        // deletion failure on shutdown is not critical
        try
        {
            await bot.DeleteWebhook(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to cleanly delete webhook on shutdown: {Message}", ex.Message);
        }
    }
}
