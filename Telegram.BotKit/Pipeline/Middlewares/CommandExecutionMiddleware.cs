using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class CommandExecutionMiddleware : ICommandMiddleware
{
    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedInvoker is not null)
        {
            await context.MatchedInvoker.InvokeAsync(context, cancellationToken);
        }

        // next() не вызываем, это конец пути
    }
}