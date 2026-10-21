using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Exceptions;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.InlineQueries.Routing;

/// <summary>
/// Routing middleware that runs every matching inline query handler and aggregates their results.
/// </summary>
/// <param name="registry">The registry of inline query handlers.</param>
/// <param name="settings">The settings of the inline query pipeline.</param>
/// <param name="serviceProvider">The service provider used to create handlers that run sequentially.</param>
/// <param name="scopeFactory">The factory of the dependency scopes in which handlers run concurrently.</param>
/// <param name="logger">The logger used to report failed handlers and button conflicts.</param>
internal sealed class InlineQueryRoutingMiddleware(
    InlineQueryHandlerRegistry registry,
    InlineQueryPipelineSettings settings,
    IServiceProvider serviceProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<InlineQueryRoutingMiddleware> logger) : IInlineQueryMiddleware
{
    private readonly record struct HandlerResult(Type HandlerType, InlineQueryBatch? Batch, Exception? Exception);

    /// <inheritdoc />
    public async ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        var matched = Match(context.Query);

        if (matched is not null)
        {
            context.IsHandled = true;

            if (matched.Count is 1 || settings.ExecutionMode is InlineQueryExecutionMode.Sequential)
            {
                await RunSequentialAsync(matched, context, cancellationToken);
            }
            else
            {
                await RunParallelAsync(matched, context, cancellationToken);
            }
        }

        await next();
    }

    private List<InlineQueryHandlerDescriptor>? Match(InlineQuery query)
    {
        List<InlineQueryHandlerDescriptor>? matched = null;
        var handlers = registry.GetHandlers();

        for (var i = 0; i < handlers.Length; i++)
        {
            if (handlers[i].CanHandle(query))
            {
                (matched ??= []).Add(handlers[i]);
            }
        }

        return matched;
    }

    private async Task RunSequentialAsync(
        List<InlineQueryHandlerDescriptor> matched,
        InlineQueryContext context,
        CancellationToken cancellationToken)
    {
        foreach (var descriptor in matched)
        {
            try
            {
                var handler = (IInlineQueryHandler)descriptor.Factory(serviceProvider);
                var batch = await handler.HandleAsync(context, cancellationToken);
                ApplyBatch(context, batch, descriptor.HandlerType);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // shutdown or aborted request is not a handler failure
                throw;
            }
            catch (Exception ex)
            {
                ReportFailure(context, descriptor.HandlerType, ex);
            }
        }
    }

    private async Task RunParallelAsync(
        List<InlineQueryHandlerDescriptor> matched,
        InlineQueryContext context,
        CancellationToken cancellationToken)
    {
        var tasks = new Task<HandlerResult>[matched.Count];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = ExecuteIsolatedAsync(matched[i], context, cancellationToken);
        }

        var results = await Task.WhenAll(tasks);

        // applied in registration order, so merging (e.g. button conflicts) is deterministic
        for (var i = 0; i < results.Length; i++)
        {
            var (handlerType, batch, exception) = results[i];

            if (exception is not null)
            {
                ReportFailure(context, handlerType, exception);
            }
            else
            {
                ApplyBatch(context, batch, handlerType);
            }
        }
    }

    private async Task<HandlerResult> ExecuteIsolatedAsync(
        InlineQueryHandlerDescriptor descriptor,
        InlineQueryContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // a dedicated scope per concurrently running handler: scoped services are not thread-safe
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = (IInlineQueryHandler)descriptor.Factory(scope.ServiceProvider);
            var batch = await handler.HandleAsync(context, cancellationToken);
            return new HandlerResult(descriptor.HandlerType, batch, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new HandlerResult(descriptor.HandlerType, null, ex);
        }
    }

    private void ReportFailure(InlineQueryContext context, Type handlerType, Exception exception)
    {
        logger.LogError(exception, "Inline query handler '{HandlerName}' failed during execution", handlerType.Name);
        context.AddException(new HandlerFailedException(handlerType, exception));
    }

    private void ApplyBatch(InlineQueryContext context, InlineQueryBatch? batch, Type handlerType)
    {
        if (batch is null)
        {
            return;
        }

        if (batch.Results is not null)
        {
            context.AddResults(batch.Results);
        }

        var buttonReplaced = context.MergeConfiguration(
            isPersonal: batch.IsPersonal,
            cacheTime: batch.CacheTime,
            nextOffset: batch.NextOffset,
            button: batch.Button);

        if (buttonReplaced)
        {
            logger.LogWarning(
                "Inline query handler '{HandlerName}' replaced the results button set by another handler. " +
                "Only one button per answer is supported; the last matched handler wins.",
                handlerType.Name);
        }
    }
}
