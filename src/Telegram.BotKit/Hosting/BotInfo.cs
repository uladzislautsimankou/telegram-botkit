using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Hosting;

internal sealed class BotInfo : IBotInfo
{
    public string Username { get; private set; } = string.Empty;

    public long Id { get; private set; }

    internal void SetState(string? username, long id)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new BotStartupException("Telegram API returned an empty Bot Username. This should not happen for bots.");
        }

        Username = username;
        Id = id;
    }
}