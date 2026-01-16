using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.BotKit.Extensions;

/// <summary>
/// Provides extension methods for simplifying message interactions within the framework context.
/// </summary>
public static class MessageExtensions
{
    /// <summary>
    /// Sends a text message as a reply to the message that triggered the current command.
    /// Automatically sets <see cref="ParseMode.Html"/> and fills <see cref="ReplyParameters"/>.
    /// </summary>
    /// <param name="bot">The Telegram bot client.</param>
    /// <param name="context">The current command context containing the source message.</param>
    /// <param name="text">The text of the message to send.</param>
    /// <param name="keyboard">Optional inline keyboard markup.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The sent <see cref="Message"/> object.</returns>
    public static Task<Message> ReplyMessage(
        this ITelegramBotClient bot,
        CommandContext context,
        string text,
        InlineKeyboardMarkup? keyboard = null,
        CancellationToken cancellationToken = default)
    {
        var replyParams = new ReplyParameters
        {
            ChatId = context.Message.Chat.Id,
            MessageId = context.Message.Id
        };

        return bot.SendMessage(
            chatId: context.Message.Chat.Id,
            text: text,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken,
            replyParameters: replyParams,
            replyMarkup: keyboard
        );
    }
}