using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Configuration;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Hosting.Webhook;

internal sealed class WebhookStartupService(
    ITelegramBotClient bot,
    BotInfo botInfo,
    IOptions<TelegramBotOptions> options,
    ILogger<WebhookStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var me = await bot.GetMe(cancellationToken: cancellationToken);
            botInfo.SetState(me.Username, me.Id, me.FirstName, me.LastName);

            logger.LogInformation("Bot identified: @{Username} (ID: {Id})", me.Username, me.Id);

            logger.LogInformation("Setting up Webhook: {Url}", options.Value.WebhookUrl);

            var allowedUpdates = options.Value.AllowedUpdates
                .Select(x => Enum.Parse<UpdateType>(x, true))
                .ToArray();

            InputFileStream? certificateInput = null;
            FileStream? certStream = null;

            if (!string.IsNullOrWhiteSpace(options.Value.CertificatePath))
            {
                if (File.Exists(options.Value.CertificatePath))
                {
                    certStream = File.OpenRead(options.Value.CertificatePath);
                    certificateInput = InputFile.FromStream(certStream, Path.GetFileName(options.Value.CertificatePath));
                    logger.LogInformation("Using self-signed certificate: {Path}", options.Value.CertificatePath);
                }
                else
                {
                    logger.LogWarning("Certificate file not found at: {Path}. Webhook will be set without certificate.", options.Value.CertificatePath);
                }
            }

            try
            {
                await bot.SetWebhook(
                    url: options.Value.WebhookUrl,
                    certificate: certificateInput,
                    ipAddress: options.Value.IpAddress,
                    maxConnections: options.Value.MaxConnections,
                    allowedUpdates: allowedUpdates,
                    dropPendingUpdates: options.Value.DropPendingUpdates,
                    secretToken: options.Value.SecretToken,
                    cancellationToken: cancellationToken);

                logger.LogInformation("✅ Webhook set successfully.");
            }
            finally
            {
                if (certStream is not null)
                    await certStream.DisposeAsync();
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

        // даже если и не удалится, то не критично
        await bot.DeleteWebhook(cancellationToken: cancellationToken);
    }
}