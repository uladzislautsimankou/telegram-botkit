namespace Telegram.BotKit.Hosting;

/// <summary>
/// Identity metadata for the running Telegram bot.
/// </summary>
public interface IBotInfo
{
    /// <summary>
    /// Bot username without the leading symbol.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Unique Telegram identifier.
    /// </summary>
    long Id { get; }

    /// <summary>
    /// First name registered in Telegram.
    /// </summary>
    string FirstName { get; }

    /// <summary>
    /// Last name registered in Telegram, or null if unset.
    /// </summary>
    string? LastName { get; }
}
