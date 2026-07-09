using Microsoft.Extensions.Logging;
using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Pipeline.Middlewares;

/// <summary>
/// A default implementation of <see cref="IInlineQueryErrorHandlerMiddleware"/> that provides
/// global error logging and handling during inline query execution.
/// </summary>
internal class DefaultInlineQueryErrorHandler(
    ILogger<DefaultInlineQueryErrorHandler> logger
    ) : IInlineQueryErrorHandlerMiddleware
{
    public async Task InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Critical error handling inline query {key}", context.Key);
        }
    }
}
