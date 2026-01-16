using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Configuration;
using Telegram.BotKit.Exceptions;

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

            botInfo.SetState(me.Username, me.Id);

            logger.LogInformation("Bot started successfully: @{Username} (ID: {Id})", me.Username, me.Id);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to connect to Telegram API. Check your Token or Network connection.");

            throw new BotStartupException("Failed to initialize bot identity.", ex);
        }

        // очистка вебхука на всякий случай
        try
        {
            await bot.DeleteWebhook(cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            // если упало удаление вебхука - это не всегда критично, но подозрительно
            logger.LogWarning("Could not delete webhook: {Message}", ex.Message);
        }

        var allowedUpdates = options.Value.AllowedUpdates
            .Select(x => Enum.Parse<UpdateType>(x, true))
            .ToArray();

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