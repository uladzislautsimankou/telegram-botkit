using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Example.Services;

/// <summary>
/// A custom update handler that replaces the framework's default handler.
/// This gives you full control over how updates are processed.
/// </summary>
public class GlobalUpdateHandler(
    IServiceProvider serviceProvider,
    ILogger<GlobalUpdateHandler> logger
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
            // Try to dispatch Commands using the Framework
            case { CallbackQuery: { } callback }:
                await callbackDispatcher.DispatchAsync(callback, cancellationToken);
                break;

            // Try to dispatch Commands using the Framework
            case { Message: { Text: string text } message } when text.StartsWith("/"):
                await commandDispatcher.DispatchAsync(message, cancellationToken);
                break;

            // Some custom handlers
            case { Message: { Text: string text } message } when !text.StartsWith("/"):
                await bot.SendMessage(update.Message.Chat.Id, $"You said: {text}. \nTry /ping", cancellationToken: cancellationToken);
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