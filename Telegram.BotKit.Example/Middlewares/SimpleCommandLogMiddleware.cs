using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// <summary>
/// Logs detailed command execution metrics prior to executing handlers.
/// </summary>
internal sealed class SimpleCommandLogMiddleware(ILogger<SimpleCommandLogMiddleware> logger) : ICommandMiddleware
{
    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Middleware: Processing command '/{Command}' from User {UserId}",
            context.Command, context.From.Id);

        // delegate to next step
        await next();

        logger.LogInformation("Middleware: Finished processing '/{Command}'", context.Command);
    }
}
