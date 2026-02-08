namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Represents the identity of the bot running the application.
/// This information is retrieved from the Telegram API during the application startup.
/// </summary>
public interface IBotInfo
{
    /// <summary>
    /// Gets the bot's username (without the '@' prefix).
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Gets the unique identifier (ID) of the bot.
    /// </summary>
    long Id { get; }

    /// <summary>
    /// Gets the bot's first name as registered in Telegram.
    /// </summary>
    string FirstName { get; }

    /// <summary>
    /// Gets the bot's last name as registered in Telegram, if provided.
    /// Returns null if the bot doesn't have a last name.
    /// </summary>
    string? LastName { get; }
}
