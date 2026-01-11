using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
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
            botInfo.SetState(me.Username, me.Id);

            logger.LogInformation("Bot identified: @{Username} (ID: {Id})", me.Username, me.Id);

            logger.LogInformation("Setting up Webhook: {Url}", options.Value.WebhookUrl);

            var allowedUpdates = options.Value.AllowedUpdates
                .Select(x => Enum.Parse<UpdateType>(x, true))
                .ToArray();

            await bot.SetWebhook(
                url: options.Value.WebhookUrl,
                allowedUpdates: allowedUpdates,
                cancellationToken: cancellationToken);

            logger.LogInformation("Webhook set successfully.");
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