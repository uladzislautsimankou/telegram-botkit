using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Hosting.Background;

/// <summary>
/// Background service that identifies the bot and receives updates with long polling.
/// </summary>
/// <param name="bot">The Telegram bot client.</param>
/// <param name="botInfo">The identity holder filled in when the bot connects.</param>
/// <param name="updateHandler">The application update handler that receives every update.</param>
/// <param name="options">The bot options.</param>
/// <param name="logger">The logger of the service.</param>
internal sealed class PollingService(
    ITelegramBotClient bot,
    BotInfo botInfo,
    IUpdateHandler updateHandler,
    IOptions<TelegramBotOptions> options,
    ILogger<PollingService> logger) : BackgroundService
{
    /// <inheritdoc />
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
            await bot.DeleteWebhook(dropPendingUpdates: botOptions.DropPendingUpdates, cancellationToken: stoppingToken);
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
