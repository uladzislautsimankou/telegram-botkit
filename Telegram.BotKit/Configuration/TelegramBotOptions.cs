namespace Telegram.BotKit.Configuration;

internal record TelegramBotOptions
{
    public string Token { get; set; } = string.Empty;

    public string[] AllowedUpdates { get; set; } = Array.Empty<string>();

    public BotMode Mode { get; set; } = BotMode.Polling;

    public string WebhookUrl { get; set; } = string.Empty;
}
