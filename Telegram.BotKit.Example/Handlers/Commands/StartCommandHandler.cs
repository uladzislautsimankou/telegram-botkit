using Telegram.Bot;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Example.Constants;

namespace Telegram.BotKit.Example.Handlers.Commands;

internal sealed class StartCommandHandler(ITelegramBotClient bot) : ICommandHandler<EmptyCommandParams>
{
    public static string Command => CommandKeys.Example.Start;

    public async Task HandleAsync(EmptyCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default)
    {
        await bot.SendMessage(
            chatId: context.Message.Chat.Id,
            text: "start command successfuly processed",
            replyParameters: context.Message.Id,
            cancellationToken: cancellationToken);
    }
}