using Telegram.Bot.Types.Enums;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Configuration options for the Telegram bot host and communication mode.
/// </summary>
internal sealed record TelegramBotOptions
{
    private string[]? _allowedUpdates;

    /// <summary>
    /// The name of the configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "TelegramBot";

    /// <summary>
    /// Gets or sets the unique authentication token of the bot obtained from @BotFather.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL of a private Bot API server. Path, query, and fragment are omitted if present.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the list of update types the bot should receive.
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
    /// Gets the strongly-typed update types parsed from <see cref="AllowedUpdates"/>.
    /// </summary>
    internal UpdateType[]? ParsedAllowedUpdates { get; private set; }

    /// <summary>
    /// Gets or sets the receiving mechanism used by the bot: long polling or webhook.
    /// </summary>
    public BotMode Mode { get; set; } = BotMode.Polling;

    /// <summary>
    /// Gets or sets the target HTTPS URL where Telegram sends updates when running in webhook mode.
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret token sent in the X-Telegram-Bot-Api-Secret-Token header with every webhook request.
    /// </summary>
    public string? SecretToken { get; set; }

    /// <summary>
    /// Gets or sets the fixed IP address used by Telegram to bypass DNS resolution for webhook requests.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed number of simultaneous HTTPS connections for update delivery (1-100).
    /// </summary>
    public int? MaxConnections { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether all pending Telegram updates are dropped upon starting the bot.
    /// </summary>
    public bool DropPendingUpdates { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the webhook is removed from Telegram when the application stops. Disabled by default:
    /// during rolling or blue/green deployments a stopping instance would otherwise delete
    /// the webhook that the new instance has just registered.
    /// Enable for single-instance setups where updates should not queue up while the bot is offline.
    /// </summary>
    public bool DeleteWebhookOnShutdown { get; set; }

    /// <summary>
    /// Gets or sets the path to a self-signed public key certificate file (e.g. cert.pem).
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
