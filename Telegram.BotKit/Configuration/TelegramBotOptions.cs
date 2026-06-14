namespace Telegram.BotKit.Configuration;

internal record TelegramBotOptions
{
    /// <summary>
    /// The unique authentication token for the bot (obtained from @BotFather).
    /// Required.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Used to change base URL to your private Bot API server.
    /// Example: "https://my-telegram-proxy.example.com". Path, query and fragment will be omitted if present.
    /// </summary>
    public string? BaseUrl {  get; set; } = null;

    /// <summary>
    /// List of update types the bot should receive (e.g., ["Message", "CallbackQuery"]).
    /// If empty, the bot receives all default update types.
    /// </summary>
    public string[] AllowedUpdates { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Determines how the bot receives updates: Long Polling or Webhook.
    /// Default is Polling.
    /// </summary>
    public BotMode Mode { get; set; } = BotMode.Polling;

    /// <summary>
    /// The HTTPS URL where Telegram sends updates.
    /// Required if Mode is set to <see cref="BotMode.Webhook"/>.
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional. A secret token to be sent in a header "X-Telegram-Bot-Api-Secret-Token" in every webhook request.
    /// Highly recommended for security.
    /// </summary>
    public string? SecretToken { get; set; }

    /// <summary>
    /// Optional. The fixed IP address which will be used to send webhook requests instead of the IP resolved through DNS.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Optional. The maximum allowed number of simultaneous HTTPS connections to the webhook for update delivery (1-100).
    /// </summary>
    public int? MaxConnections { get; set; }

    /// <summary>
    /// Optional. Pass True to drop all pending updates.
    /// </summary>
    public bool DropPendingUpdates { get; set; } = false;

    /// <summary>
    /// Optional. Path to the public key certificate file (e.g. "cert.pem") if you use a self-signed certificate.
    /// </summary>
    public string? CertificatePath { get; set; }
}
