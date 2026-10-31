using System.Net;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.Enums;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Validates configuration options for the Telegram bot on application startup.
/// </summary>
internal sealed class TelegramBotOptionsValidator : IValidateOptions<TelegramBotOptions>
{
    private const int MaxSecretTokenLength = 256;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, TelegramBotOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Token))
        {
            failures.Add("TelegramBot:Token is required and cannot be empty.");
        }

        if (!string.IsNullOrWhiteSpace(options.BaseUrl) && !IsHttpUrl(options.BaseUrl))
        {
            failures.Add($"TelegramBot:BaseUrl '{options.BaseUrl}' must be a valid absolute HTTP(S) URL.");
        }

        if (options.Mode is BotMode.Webhook)
        {
            if (string.IsNullOrWhiteSpace(options.WebhookUrl))
            {
                failures.Add("TelegramBot:WebhookUrl is required when Mode is set to Webhook.");
            }
            else if (!Uri.TryCreate(options.WebhookUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                failures.Add($"TelegramBot:WebhookUrl '{options.WebhookUrl}' must be a valid absolute HTTPS URL.");
            }
        }

        if (!string.IsNullOrEmpty(options.SecretToken) && !IsValidSecretToken(options.SecretToken))
        {
            failures.Add($"TelegramBot:SecretToken must be 1-{MaxSecretTokenLength} characters long " +
                         "and contain only A-Z, a-z, 0-9, '_' and '-'.");
        }

        if (!string.IsNullOrWhiteSpace(options.IpAddress) && !IPAddress.TryParse(options.IpAddress, out _))
        {
            failures.Add($"TelegramBot:IpAddress '{options.IpAddress}' is not a valid IP address.");
        }

        if (options.MaxConnections is < 1 or > 100)
        {
            failures.Add($"TelegramBot:MaxConnections must be between 1 and 100, but was {options.MaxConnections}.");
        }

        if (options.AllowedUpdates is { Length: > 0 } updates)
        {
            var unknown = updates.Where(u => !IsKnownUpdateType(u)).ToArray();
            if (unknown.Length > 0)
            {
                failures.Add($"TelegramBot:AllowedUpdates contains unknown update types: {string.Join(", ", unknown)} " +
                             "(expected UpdateType names, e.g. 'Message', 'CallbackQuery').");
            }
        }

        return failures.Count is 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidSecretToken(string token) =>
        token.Length <= MaxSecretTokenLength
        && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

    private static bool IsKnownUpdateType(string value) =>
        Enum.TryParse<UpdateType>(value, ignoreCase: true, out var type)
        && type != UpdateType.Unknown
        && Enum.IsDefined(type);

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
