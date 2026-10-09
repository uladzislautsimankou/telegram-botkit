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
                ApplyBatch(context, batch, handler.GetType().Name);
            }
            catch (Exception ex)
            {
                context.AddException(new HandlerFailedException(handler.GetType(), ex));
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

    private void ApplyBatch(InlineQueryContext context, InlineQueryBatch batch, string handlerName)
    {
        context.AddResults(batch.Results);

        if (batch.Button is not null && context.HasButton)
        {
            logger.LogWarning(
                "Conflict: Multiple inline query handlers provided a button. " +
                "Overwriting previous button with the one from '{HandlerName}'",
                handlerName);
        }

        context.MergeConfiguration(
            isPersonal: batch.IsPersonal,
            cacheTime: batch.CacheTime,
            nextOffset: batch.NextOffset,
            button: batch.Button);
    }

    private async Task InvokeHandlerSafeAsync(InlineQueryContext context, IInlineQueryHandler handler, CancellationToken cancellationToken)
    {
        try
        {
            var batch = await handler.HandleAsync(context, cancellationToken);
            ApplyBatch(context, batch, handler.GetType().Name);
        }
        catch (Exception ex)
        {
            context.AddException(new HandlerFailedException(handler.GetType(), ex));
        }
    }
}