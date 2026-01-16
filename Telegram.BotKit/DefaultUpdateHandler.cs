using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit;

internal sealed class DefaultUpdateHandler(
    IServiceProvider serviceProvider,
    ILogger<DefaultUpdateHandler> logger
    ) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Received update of type {UpdateType}.", update.Type);

        await using var scope = serviceProvider.CreateAsyncScope();

        var callbackDispatcher = scope.ServiceProvider.GetRequiredService<ICallbackDispatcher>();
        var commandDispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        switch (update)
        {
            case { CallbackQuery: { } callback }:
                logger.LogDebug("Routing callback query with data: {CallbackData}", callback.Data);
                await callbackDispatcher.DispatchAsync(callback, cancellationToken);
                break;

            case { Message: { Text: string text } message } when text.StartsWith("/"):
                logger.LogDebug("Routing command: {CommandText}", text);
                await commandDispatcher.DispatchAsync(message, cancellationToken);
                break;

            default:
                break;
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken = default)
    {
        logger.LogError(exception, "Error occurred in Telegram bot update pipeline. Source: {Source}", source);
        return Task.CompletedTask;
    }
}
