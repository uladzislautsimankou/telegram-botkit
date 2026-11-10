using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Example.Constants;
using Telegram.BotKit.Example.Services;
using Telegram.BotKit.Example.Widgets;

namespace Telegram.BotKit.Example.Handlers.Commands;

/// <summary>
/// Sends the counter widget. Its taps come back as widget queries handled by CounterWidgetQueryHandler.
/// </summary>
internal sealed class WidgetCommandHandler(ITelegramBotClient bot) : ICommandHandler<EmptyCommandParams>
{
    // the HTML of a sent widget never changes, so it is read and checked once
    private static readonly Lazy<string> CounterHtml = new(() => WidgetHtml.Load("counter.html"));

    public static string Command => CommandKeys.Example.Widget;

    public async Task HandleAsync(EmptyCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default)
    {
        var chat = context.Message.Chat;

        // the simplest case: a private chat, where the chat state belongs to one user;
        // which other chats accept widgets is up to Telegram
        if (chat.Type != ChatType.Private)
        {
            await bot.SendMessage(
                chatId: chat.Id,
                text: "Send /widget in a private chat with me.",
                replyParameters: context.Message.Id,
                cancellationToken: cancellationToken);
            return;
        }

        await bot.SendWidget(
            chatId: chat.Id,
            html: CounterHtml.Value,
            allowQueries: true, // without it Telegram refuses every request of the widget
            chatData: CounterStore.ToState(0), // the state the widget draws first
            cancellationToken: cancellationToken);
    }
}
