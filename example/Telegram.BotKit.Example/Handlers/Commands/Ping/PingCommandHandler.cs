using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Attributes;
using Telegram.BotKit.Example.Constants;
using Telegram.BotKit.Extensions;

namespace Telegram.BotKit.Example.Handlers.Commands.Ping;

internal record PingCommandParams
{
    // Required positional argument (index 0). Example: /ping "some text"
    [CommandParam(Position = 0, Required = true)]
    public string? Message { get; init; }

    // Named argument. Example: /ping c=5
    [CommandParam(Name = "c")]
    public int? Count { get; init; }
}

internal sealed class PingCommandHandler(ITelegramBotClient bot) : ICommandHandler<PingCommandParams>
{
    public string Command => CommandKeys.Example.Ping;

    public async Task HandleAsync(PingCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default)
    {
        var responseText = 
            $"🏓 <b>Pong!</b>\n" +
            $"Message: {parameters.Message}\n" +
            $"Count: {parameters.Count ?? 0}";

        // Prepare parameters for the callback button
        // URL: ping:click?m=...&c=...
        var callbackData = $"{CallbackKeys.Example.Ping}?m={parameters.Message}&c={parameters.Count}";

        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Test Callback", callbackData)
        );

        // Reply using the framework extension method
        await bot.ReplyMessage(context, responseText, keyboard, cancellationToken);
    }
}