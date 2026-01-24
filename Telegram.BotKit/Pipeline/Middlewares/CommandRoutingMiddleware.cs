using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Helpers;
using Telegram.BotKit.Invocation;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class CommandRoutingMiddleware(
    IEnumerable<ICommandHandlerInvoker> invokers,
    IBotInfo botInfo
    ) : ICommandMiddleware
{
    private readonly Dictionary<string, ICommandHandlerInvoker> _handlerMap
        = invokers.ToDictionary(x => x.Command, x => x, StringComparer.OrdinalIgnoreCase);

    private readonly CommandSearcher _searcher = new CommandSearcher(invokers.Select(x => x.Command));


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
            // ищем похожие команды
            var similar = _searcher.FindSimilar(context.Command);

            if (similar.Count > 0)
            {
                throw new CommandSuggestionsException(context.Command, similar);
            }

            // в приватных выбрасываем обычную ошибку
            if (context.Message.Chat.Type == ChatType.Private)
            {
                throw new RouteNotFoundException(context.Command);
            }
            
            // в остальных чатах игнорируем, вероятно не нам команда
            return;
        }

        await invoker.InvokeAsync(context, cancellationToken);

        // next() не вызываем, это конец пути
    }
}
