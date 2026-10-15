using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Hosting.Webhook;

internal sealed class WebhookStartupService(
    ITelegramBotClient bot,
    BotInfo botInfo,
    IOptions<TelegramBotOptions> options,
    ILogger<WebhookStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var botOptions = options.Value;

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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
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
