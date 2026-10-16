namespace Telegram.BotKit.Hosting;

/// <summary>
/// Specifies how the bot receives updates from Telegram.
/// </summary>
internal enum BotMode
{
    /// <summary>
    /// The bot requests updates from Telegram with long polling.
    /// </summary>
    Polling,
    /// <summary>
    /// Telegram sends updates to an HTTPS endpoint of the bot.
    /// </summary>
    Webhook
}

