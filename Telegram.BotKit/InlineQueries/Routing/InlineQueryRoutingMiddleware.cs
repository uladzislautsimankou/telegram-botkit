using Microsoft.Extensions.Logging;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.InlineQueries.Routing;

/// <summary>
/// Routing middleware responsible for identifying and executing matching inline query handlers.
/// </summary>
internal sealed class InlineQueryRoutingMiddleware(
    IEnumerable<IInlineQueryHandler> handlers,
    ILogger<InlineQueryRoutingMiddleware> logger
    ) : IInlineQueryMiddleware
{
    public async Task InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
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

        // if only one handler matched, invoke it directly to avoid Task.WhenAll allocations
        if (activeHandlers.Count is 1)
        {
            var handler = activeHandlers[0];
            try
            {
                var batch = await handler.HandleAsync(context, cancellationToken);
                PopulateContext(context, batch, handler.GetType().Name);
            }
            catch (Exception ex)
            {
                context.Exceptions.Add(new HandlerFailedException(handler.GetType(), ex));
            }
        }
        else
        {
            var tasks = new Task[activeHandlers.Count];
            for (var i = 0; i < activeHandlers.Count; i++)
            {
                var handler = activeHandlers[i];
                tasks[i] = InvokeHandlerSafeAsync(context, handler, cancellationToken);
            }

            await Task.WhenAll(tasks);
        }

        await next();
    }

    private void PopulateContext(InlineQueryContext context, InlineQueryBatch batch, string handlerName)
    {
        lock (context.Results)
        {
            context.Results.AddRange(batch.Results);
            if (batch.IsPersonal)
            {
                context.IsPersonal = true;
            }
            if (batch.CacheTime is not null && (context.CacheTime is null || batch.CacheTime < context.CacheTime))
            {
                context.CacheTime = batch.CacheTime;
            }
            if (!string.IsNullOrEmpty(batch.NextOffset))
            {
                context.NextOffset = batch.NextOffset;
            }
            if (batch.Button is not null)
            {
                if (context.Button is not null)
                {
                    logger.LogWarning(
                        "Conflict: Multiple inline query handlers provided a button. " +
                        "Overwriting previous button with the one from '{HandlerName}'",
                        handlerName);
                }
                context.Button = batch.Button;
            }
        }
    }

    private async Task InvokeHandlerSafeAsync(InlineQueryContext context, IInlineQueryHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            var batch = await handler.HandleAsync(context, cancellationToken);
            PopulateContext(context, batch, handler.GetType().Name);
        }
        catch (Exception ex)
        {
            lock (context.Exceptions)
            {
                context.Exceptions.Add(new HandlerFailedException(handler.GetType(), ex));
            }
        }
    }
}