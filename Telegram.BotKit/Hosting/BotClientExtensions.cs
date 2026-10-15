using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram.BotKit.Hosting;

internal static class BotClientExtensions
{
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
