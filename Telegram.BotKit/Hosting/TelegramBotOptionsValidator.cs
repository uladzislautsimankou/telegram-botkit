using Microsoft.Extensions.Options;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Validates configuration options for the Telegram bot on application startup.
/// </summary>
internal sealed class TelegramBotOptionsValidator : IValidateOptions<TelegramBotOptions>
{
    public ValidateOptionsResult Validate(string? name, TelegramBotOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Token))
        {
            failures.Add("TelegramBot:Token is required and cannot be empty.");
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

        if (options.MaxConnections is < 1 or > 100)
        {
            failures.Add($"TelegramBot:MaxConnections must be between 1 and 100, but was {options.MaxConnections}.");
        }

        return failures.Count is 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
