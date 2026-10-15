using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;

namespace Telegram.BotKit.Example.Handlers.Messages;

/// <summary>
/// Handles repeat messages in private chats, echoing back the text after the trigger.
/// </summary>
internal sealed class SimpleTextHandler(ITelegramBotClient bot) : IMessageHandler
{
    private const string Trigger = "repeat";

    public static bool CanHandle(MessageContext context) =>
        context.Message is { Chat.Type: ChatType.Private, Text: string text }
        && text.StartsWith(Trigger, StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var message = context.Message;
        var text = message.Text!;

        var replyText = text[Trigger.Length..].Trim();

        if (string.IsNullOrWhiteSpace(replyText))
        {
            replyText = "You didn't say anything";
        }

        await bot.SendMessage(
            chatId: context.Message.Chat.Id,
            text: replyText,
            replyParameters: context.Message.Id,
            cancellationToken: cancellationToken);
    }
}
