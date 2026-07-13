using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
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
        try
        {
            var me = await bot.GetMe(cancellationToken: stoppingToken);

            botInfo.SetState(me.Username, me.Id, me.FirstName, me.LastName);

            logger.LogInformation("Bot started successfully: @{Username} (ID: {Id})", me.Username, me.Id);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to connect to Telegram API. Check your Token or Network connection.");

            throw new BotStartupException("Failed to initialize bot identity.", ex);
        }

        // clear webhook just in case
        try
        {
            await bot.DeleteWebhook(cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            // a failed webhook deletion is not always critical, but it is suspicious, so we log it as a warning
            logger.LogWarning("Could not delete webhook: {Message}", ex.Message);
        }

        // Null: Keep previous settings. Array.Empty: Reset to Telegram defaults.
        var allowedUpdates = options.Value.AllowedUpdates is not null
            ? options.Value.AllowedUpdates.Select(x => Enum.Parse<UpdateType>(x, true)).ToArray()
            : null;

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = allowedUpdates,
            DropPendingUpdates = false
        };

        logger.LogInformation("Starting polling...");

        await bot.ReceiveAsync(
            updateHandler: updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }
}