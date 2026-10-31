using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Logs detailed command execution metrics prior to executing handlers.
/// </summary>
internal sealed class SimpleCommandLogMiddleware(ILogger<SimpleCommandLogMiddleware> logger) : ICommandMiddleware
{
    public async ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Middleware: Processing command '/{Command}' from User {UserId}",
            context.Command, context.Message.From?.Id);

        await next();

        logger.LogInformation("Middleware: Finished processing '/{Command}'", context.Command);
    }
}
