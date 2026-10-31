using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.BotKit.Callbacks.Building;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Attributes;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Example.Constants;
using Telegram.BotKit.Example.Handlers.Callbacks;

namespace Telegram.BotKit.Example.Handlers.Commands;

internal record PingCommandParams
{
    // required positional argument (index 0). Example: /ping "some text"
    [CommandParam(Position = 0, Required = true)]
    public string? Message { get; init; }

    // named argument. Example: /ping c=5
    [CommandParam(Name = "c")]
    public int? Count { get; init; }

    [CommandParam(Name = "d")]
    public DateTime Date { get; init; }
}

/// <summary>
/// Responds with a pong notification and returns a keyboard markup bound to a query callback trigger.
/// </summary>
internal sealed class PingCommandHandler(ITelegramBotClient bot) : ICommandHandler<PingCommandParams>
{
    public static string Command => CommandKeys.Example.Ping;

    public async Task HandleAsync(PingCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default)
    {
        var responseText =
            $"🏓 <b>Pong!</b>\n" +
            $"Message: {parameters.Message}\n" +
            $"Count: {parameters.Count ?? 0}\n" +
            $"Date: {parameters.Date.ToShortDateString()}";

        var keyboard = new InlineKeyboardMarkup(
        [
            [
                CallbackButton.Create<PingCallbackHandler, PingCallbackParams>(
                    "Test Callback",
                    new PingCallbackParams { Message = parameters.Message, Count = parameters.Count ?? 0 }),
            ]
        ]);

        await bot.SendMessage(
            chatId: context.Message.Chat.Id,
            text: responseText,
            replyParameters: context.Message.Id,
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}
