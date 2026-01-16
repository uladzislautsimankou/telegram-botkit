using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Telegram.BotKit.Hosting.Webhook;

[ApiController]
[Route("api/bot")]
public class BotController(IUpdateHandler updateHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update, CancellationToken ct)
    {
        // null в качестве botClient, т.к. обычно IUpdateHandler из либы Telegram.Bot ожидает его,
        // но мы инжектим botClient прямо в хендлеры.
        await updateHandler.HandleUpdateAsync(null!, update, ct);

        return Ok();
    }
}
