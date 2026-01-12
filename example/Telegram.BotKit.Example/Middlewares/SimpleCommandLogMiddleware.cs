using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// A simple middleware that logs every command execution.
/// </summary>
public class SimpleCommandLogMiddleware(ILogger<SimpleCommandLogMiddleware> logger) : ICommandMiddleware
{
    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Middleware: Processing command '/{Command}' from User {UserId}",
            context.Command, context.From.Id);

        // Pass control to the next middleware (or the router)
        await next();

        logger.LogInformation("Middleware: Finished processing '/{Command}'", context.Command);
    }
}
