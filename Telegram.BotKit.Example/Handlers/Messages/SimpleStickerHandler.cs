using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;

namespace Telegram.BotKit.Example.Handlers.Messages;

/// <summary>
/// Handles incoming stickers in private chats by echoing the same sticker back.
/// </summary>
internal sealed class SimpleStickerHandler(ITelegramBotClient bot) : IMessageHandler
{
    public bool CanHandle(MessageContext context) =>
        context.Message is { Chat.Type: ChatType.Private, Sticker: { } };

    public async Task HandleAsync(MessageContext context, CancellationToken cancellationToken)
    {
        await bot.SendSticker(
            chatId: context.Message.Chat.Id,
            sticker: InputFile.FromFileId(context.Message.Sticker!.FileId),
            cancellationToken: cancellationToken);
    }
}
