using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.BotKit.Example.Constants;
using Telegram.BotKit.Example.Services;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Attributes;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.Example.Handlers.WidgetQueries;

internal record CounterWidgetQueryParams
{
    // short alias "by" to save space: the data of a widget query is limited to 64 bytes
    [WidgetParam(Name = "by")]
    public int By { get; init; } = 1;
}

/// <summary>
/// Adds to the counter when the widget asks for it, and answers with the new state at once.
/// </summary>
internal sealed class CounterWidgetQueryHandler(
    ITelegramBotClient bot,
    CounterStore store,
    ILogger<CounterWidgetQueryHandler> logger
) : IWidgetQueryHandler<CounterWidgetQueryParams>
{
    public static string Key => WidgetQueryKeys.Example.CounterAdd;

    public async Task HandleAsync(CounterWidgetQueryParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default)
    {
        // only a tap in the original message knows its chat and message; a copy and a message sent via the bot do not
        if (context.Chat is not { } chat || context.MessageId is not { } messageId)
        {
            await bot.AnswerWidgetQuery(widgetQueryId: context.Id, cancellationToken: cancellationToken);
            return;
        }

        // the data comes from the widget, which the user can fake: keep it in the range the buttons send
        var total = store.Add(chat.Id, messageId, Math.Clamp(parameters.By, 1, 5));
        var state = CounterStore.ToState(total);

        // an answer carries a text for the widget that asked and never changes its state;
        // it comes first, so that the widget is not left waiting for the 15 seconds Telegram gives the bot
        await bot.AnswerWidgetQuery(widgetQueryId: context.Id, data: state, cancellationToken: cancellationToken);

        // the state of the message is changed by the bot itself: it persists, and every device shows it
        try
        {
            await bot.EditMessageWidgetData(
                chatId: chat.Id,
                messageId: messageId,
                chatData: state,
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            // e.g. "message is not modified" or a flood limit: the widget has its answer already
            logger.LogWarning(ex, "The state of widget message {MessageId} was not saved", messageId);
        }
    }
}
