using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Invocation;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class CommandRoutingMiddleware(
    IEnumerable<ICommandHandlerInvoker> invokers,
    IBotInfo botInfo
    ) : ICommandMiddleware
{
    private readonly Dictionary<string, ICommandHandlerInvoker> _handlerMap
        = invokers.ToDictionary(x => x.Command, x => x, StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(context.TargetBotUsername)
            && !context.TargetBotUsername.Equals(botInfo.Username, StringComparison.OrdinalIgnoreCase))
        {
            // комманда вообще не для нашего бота
            return;
        }

        if (!_handlerMap.TryGetValue(context.Command, out var invoker))
        {
            //комманду не нашли
            throw new RouteNotFoundException(context.Command);
        }

        await invoker.InvokeAsync(context, cancellationToken);

        // next() не вызываем, это конец пути
    }
}
