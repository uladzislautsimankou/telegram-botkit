using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Telegram.BotKit.Hosting.Webhook;

[ApiController]
[Route("api/bot")]
public class BotController(IUpdateHandler updateHandler, ITelegramBotClient bot) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update, CancellationToken ct)
    {
        await updateHandler.HandleUpdateAsync(bot, update, ct);

        return Ok();
    }
}
