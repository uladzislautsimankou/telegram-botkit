using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.Messages.Abstractions;

namespace Telegram.BotKit.Example.Services;

/// <summary>
/// Intercepts and routes update events before handing them off to individual framework dispatchers.
/// </summary>
public class GlobalUpdateHandler(
    ICommandDispatcher commandDispatcher,
    ICallbackDispatcher callbackDispatcher,
    IMessageDispatcher messageDispatcher,
    IInlineQueryDispatcher inlineQueryDispatcher,
    IChosenInlineResultDispatcher chosenInlineResultDispatcher,
    ILogger<GlobalUpdateHandler> logger
    ) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Received update of type {UpdateType}.", update.Type);

        switch (update)
        {
            // dispatch callback queries
            case { CallbackQuery: { } callback }:
                await callbackDispatcher.DispatchAsync(callback, cancellationToken);
                break;

            // dispatch commands starting with slash
            case { Message: { Text: string text } message } when text.StartsWith("/"):
                await commandDispatcher.DispatchAsync(message, cancellationToken);
                break;

            // dispatch standard message updates
            case { Message: { } message }:
                await messageDispatcher.DispatchAsync(message, cancellationToken);
                break;

            // dispatch inline queries
            case { InlineQuery: { } inlineQuery }:
                await inlineQueryDispatcher.DispatchAsync(inlineQuery, cancellationToken);
                break;

            // dispatch chosen inline results
            case { ChosenInlineResult: { } chosenResult }:
                await chosenInlineResultDispatcher.DispatchAsync(chosenResult, cancellationToken);
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


