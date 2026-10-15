using Telegram.Bot.Types.Enums;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Configuration options for the Telegram bot host and communication mode.
/// </summary>
internal sealed record TelegramBotOptions
{
    private string[]? _allowedUpdates;

    public const string SectionName = "TelegramBot";

    /// <summary>
    /// Unique authentication token for the bot obtained from @BotFather.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for a private Bot API server. Path, query, and fragment are omitted if present.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// List of update types the bot should receive.
    /// Passing null retains previous settings; empty array resets to defaults.
    /// </summary>
    public string[]? AllowedUpdates
    {
        get => _allowedUpdates;
        set
        {
            _allowedUpdates = value;
            ParsedAllowedUpdates = ParseUpdates(value);
        }
    }

    /// <summary>
    /// Strongly-typed update types parsed from <see cref="AllowedUpdates"/>.
    /// </summary>
    internal UpdateType[]? ParsedAllowedUpdates { get; private set; }

    /// <summary>
    /// Receiving mechanism used by the bot: Long Polling or Webhook.
    /// </summary>
    public BotMode Mode { get; set; } = BotMode.Polling;

    /// <summary>
    /// Target HTTPS URL where Telegram sends updates when running in webhook mode.
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Secret token sent in the X-Telegram-Bot-Api-Secret-Token header with every webhook request.
    /// </summary>
    public string? SecretToken { get; set; }

    /// <summary>
    /// Fixed IP address used by Telegram to bypass DNS resolution for webhook requests.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Maximum allowed number of simultaneous HTTPS connections for update delivery (1-100).
    /// </summary>
    public int? MaxConnections { get; set; }

    /// <summary>
    /// Drops all pending Telegram updates upon starting the bot.
    /// </summary>
    public bool DropPendingUpdates { get; set; }

    /// <summary>
    /// Path to a self-signed public key certificate file (e.g. cert.pem).
    /// </summary>
    public string? CertificatePath { get; set; }

    private static UpdateType[]? ParseUpdates(string[]? updates)
    {
        if (updates is null)
        {
            return null;
        }

        var result = new List<UpdateType>(updates.Length);
        foreach (var update in updates)
        {
            if (Enum.TryParse<UpdateType>(update, ignoreCase: true, out var parsed))
            {
                result.Add(parsed);
            }
        }

        return result.ToArray();
    }
}
