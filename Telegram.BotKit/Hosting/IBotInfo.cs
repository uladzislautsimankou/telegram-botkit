namespace Telegram.BotKit.Hosting;

/// <summary>
/// Identity metadata for the running Telegram bot.
/// </summary>
public interface IBotInfo
{
    /// <summary>
    /// Gets the bot username without the leading '@'.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Gets the unique Telegram identifier of the bot.
    /// </summary>
    long Id { get; }

    /// <summary>
    /// Gets the first name registered in Telegram.
    /// </summary>
    string FirstName { get; }

    /// <summary>
    /// Gets the last name registered in Telegram, or null if unset.
    /// </summary>
    string? LastName { get; }
}
