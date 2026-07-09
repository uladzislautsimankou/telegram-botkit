using System.Text.Json.Serialization;
using Telegram.Bot;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Example.Constants;

namespace Telegram.BotKit.Example.Handlers.Callbacks;

internal record PingCallbackParams
{
    // short alias "m" to save space in callback_data
    [JsonPropertyName("m")]
    public string? Message { get; init; }

    [JsonPropertyName("c")]
    public int? Count { get; init; }
}

/// <summary>
/// Displays callback arguments in alert notifications when users tap keyboard query triggers.
/// </summary>
internal sealed class PingCallbackHandler(ITelegramBotClient bot) : ICallbackHandler<PingCallbackParams>
{
    public string Key => CallbackKeys.Example.Ping;

    public async Task HandleAsync(PingCallbackParams parameters, CallbackContext context, CancellationToken cancellationToken = default)
    {
        // show a toast notification to the user
        await bot.AnswerCallbackQuery(
            callbackQueryId: context.Query.Id,
            text: $"Callback received! Msg: {parameters.Message}, Count: {parameters.Count}",
            showAlert: true,
            cancellationToken: cancellationToken);
    }
}