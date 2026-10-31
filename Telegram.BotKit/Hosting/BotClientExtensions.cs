using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Extension methods for the Telegram bot client used by the bot host.
/// </summary>
internal static class BotClientExtensions
{
    /// <summary>
    /// Requests the identity of the bot from Telegram and stores it in <paramref name="botInfo"/>.
    /// </summary>
    /// <param name="bot">The Telegram bot client.</param>
    /// <param name="botInfo">The identity holder to fill in.</param>
    /// <param name="logger">The logger that reports the identified bot.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The bot user returned by Telegram.</returns>
    /// <exception cref="BotStartupException">Thrown when Telegram returns an empty bot username.</exception>
    public static async Task<User> InitializeIdentityAsync(
        this ITelegramBotClient bot,
        BotInfo botInfo,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var me = await bot.GetMe(cancellationToken: cancellationToken);
        botInfo.SetState(me.Username, me.Id, me.FirstName, me.LastName);

        logger.LogInformation("Bot identified: @{Username} (ID: {Id})", me.Username, me.Id);
        return me;
    }
}
