using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Catches exceptions of the inline query pipeline (for example a failed answer to Telegram) and logs them,
/// so they do not reach the update handler.
/// A failing inline handler does not throw: its exception is logged by the framework and collected in
/// <see cref="InlineQueryContext.Exceptions"/>, and a query that no handler matched is answered with an empty list.
/// </summary>
internal sealed class MyCustomInlineQueryErrorHandler(
    ILogger<MyCustomInlineQueryErrorHandler> logger
    ) : IInlineQueryMiddleware
{
    public async ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Critical error handling inline query {QueryId}", context.Query.Id);
        }
    }
}
