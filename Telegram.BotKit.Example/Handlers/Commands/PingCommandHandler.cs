using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Attributes;
using Telegram.BotKit.Example.Constants;
using Telegram.BotKit.Extensions;

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
    public string Command => CommandKeys.Example.Ping;

    public async Task HandleAsync(PingCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default)
    {
        var responseText =
            $"🏓 <b>Pong!</b>\n" +
            $"Message: {parameters.Message}\n" +
            $"Count: {parameters.Count ?? 0}\n" +
            $"Date: {parameters.Date.ToShortDateString()}";

        // prepare parameters for the callback button
        // URL format: ping:click?m=...&c=...
        var callbackData = $"{CallbackKeys.Example.Ping}?m={parameters.Message}&c={parameters.Count}";

        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Test Callback", callbackData)
        );

        // reply using the framework extension method
        await bot.ReplyMessage(context, responseText, keyboard, cancellationToken: cancellationToken);
    }
}