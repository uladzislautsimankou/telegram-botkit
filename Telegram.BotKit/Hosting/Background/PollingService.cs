using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Hosting.Background;

internal sealed class PollingService(
    ITelegramBotClient bot,
    BotInfo botInfo,
    IUpdateHandler updateHandler,
    IOptions<TelegramBotOptions> options,
    ILogger<PollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botOptions = options.Value;

        try
        {
            await bot.InitializeIdentityAsync(botInfo, logger, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to connect to Telegram API. Check your Token or Network connection.");
            throw new BotStartupException("Failed to initialize bot identity.", ex);
        }

        // drop pre-existing webhook to unblock polling
        try
        {
            await bot.DeleteWebhook(cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Could not delete webhook: {Message}", ex.Message);
        }

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = botOptions.ParsedAllowedUpdates,
            DropPendingUpdates = botOptions.DropPendingUpdates
        };

        logger.LogInformation("Starting polling...");

        await bot.ReceiveAsync(
            updateHandler: updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }
}
