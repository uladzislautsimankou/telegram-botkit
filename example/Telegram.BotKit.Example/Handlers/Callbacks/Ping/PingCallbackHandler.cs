using System.Text.Json.Serialization;
using Telegram.Bot;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Example.Constants;

namespace Telegram.BotKit.Example.Handlers.Callbacks.Ping;

internal record PingCallbackParams
{
    // Short alias "m" to save space in callback_data
    [JsonPropertyName("m")]
    public string? Message { get; init; }

    [JsonPropertyName("c")]
    public int? Count { get; init; }
}

internal sealed class PingCallbackHandler(ITelegramBotClient bot) : ICallbackHandler<PingCallbackParams>
{
    public string Key => CallbackKeys.Example.Ping;

    public async Task HandleAsync(PingCallbackParams parameters, CallbackContext context, CancellationToken cancellationToken = default)
    {
        // Show a toast notification to the user
        await bot.AnswerCallbackQuery(
            callbackQueryId: context.Query.Id,
            text: $"Callback received! Msg: {parameters.Message}, Count: {parameters.Count}",
            showAlert: true,
            cancellationToken: cancellationToken);
    }
}
