using Microsoft.Extensions.Logging;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.InlineQueries.Routing;

internal sealed class InlineQueryRoutingMiddleware(
    IEnumerable<IInlineQueryHandler> handlers,
    ILogger<InlineQueryRoutingMiddleware> logger) : IInlineQueryMiddleware
{
    public async ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        var activeHandlers = new List<IInlineQueryHandler>();

        foreach (var handler in handlers)
        {
            if (handler.CanHandle(context.Query))
            {
                activeHandlers.Add(handler);
            }
        }

        if (activeHandlers.Count is 0)
        {
            await next();
            return;
        }

        context.IsHandled = true;

        // fast path: execute directly when only a single handler matched
        if (activeHandlers.Count is 1)
        {
            var handler = activeHandlers[0];
            try
            {
                var batch = await handler.HandleAsync(context, cancellationToken);
                ApplyBatch(context, batch, handler.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Inline query handler '{HandlerName}' failed during execution", handler.GetType().Name);
                context.AddException(new HandlerFailedException(handler.GetType(), ex));
            }

            await next();
            return;
        }

        // fan-out: execute all matching handlers concurrently
        var tasks = new Task<(IInlineQueryHandler Handler, InlineQueryBatch? Batch, Exception? Exception)>[activeHandlers.Count];
        for (var i = 0; i < activeHandlers.Count; i++)
        {
            tasks[i] = ExecuteHandlerAsync(activeHandlers[i], context, cancellationToken);
        }

        var results = await Task.WhenAll(tasks);

        for (var i = 0; i < results.Length; i++)
        {
            var (handler, batch, exception) = results[i];

            if (exception is not null)
            {
                logger.LogError(exception, "Inline query handler '{HandlerName}' failed during execution", handler.GetType().Name);
                context.AddException(new HandlerFailedException(handler.GetType(), exception));
            }
            else
            {
                ApplyBatch(context, batch, handler.GetType().Name);
            }
        }

        await next();
    }

    private static async Task<(IInlineQueryHandler Handler, InlineQueryBatch? Batch, Exception? Exception)> ExecuteHandlerAsync(
        IInlineQueryHandler handler,
        InlineQueryContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var batch = await handler.HandleAsync(context, cancellationToken);
            return (handler, batch, null);
        }
        catch (Exception ex)
        {
            return (handler, null, ex);
        }
    }

    private void ApplyBatch(InlineQueryContext context, InlineQueryBatch? batch, string handlerName)
    {
        if (batch is null)
        {
            return;
        }

        if (batch.Results is not null)
        {
            context.AddResults(batch.Results);
        }

        context.MergeConfiguration(
            isPersonal: batch.IsPersonal,
            cacheTime: batch.CacheTime,
            nextOffset: batch.NextOffset,
            button: batch.Button);
    }
}
