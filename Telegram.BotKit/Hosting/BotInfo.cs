using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// The identity of the running bot; filled in when the bot connects to Telegram.
/// </summary>
internal sealed class BotInfo : IBotInfo
{
    /// <inheritdoc />
    public string Username { get; private set; } = string.Empty;

    /// <inheritdoc />
    public long Id { get; private set; }

    /// <inheritdoc />
    public string FirstName { get; private set; } = string.Empty;

    /// <inheritdoc />
    public string? LastName { get; private set; }

    /// <summary>
    /// Stores the identity returned by Telegram.
    /// </summary>
    /// <param name="username">The bot username.</param>
    /// <param name="id">The unique Telegram identifier of the bot.</param>
    /// <param name="firstName">The first name registered in Telegram.</param>
    /// <param name="lastName">The last name registered in Telegram, or null if unset.</param>
    /// <exception cref="BotStartupException">Thrown when the username is empty.</exception>
    internal void SetState(string? username, long id, string firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BotStartupException("Telegram API returned an empty Bot Username. This should not happen for bots.");
        }

        Username = username;
        Id = id;
        FirstName = firstName;
        LastName = lastName;
    }
}
