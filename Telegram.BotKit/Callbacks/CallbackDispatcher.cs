using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Callbacks;

/// <summary>
/// Dispatches callback queries by executing them through the configured middleware pipeline.
/// </summary>
internal sealed class CallbackDispatcher(
    IServiceProvider serviceProvider,
    CallbackPipelineBuilder pipelineBuilder
    ) : ICallbackDispatcher
{
    public async Task DispatchAsync(CallbackQuery query, CancellationToken cancellationToken = default)
    {
        var context = new CallbackContext(query);

        using var scope = serviceProvider.CreateScope();

        // resolve middlewares from DI dynamically based on the builder configuration order
        var steps = pipelineBuilder.Steps;
        var resolvedMiddlewares = new ICallbackMiddleware[steps.Count];

        for (var i = 0; i < steps.Count; i++)
        {
            resolvedMiddlewares[i] = (ICallbackMiddleware)scope.ServiceProvider.GetRequiredService(steps[i].MiddlewareType);
        }

        var runner = new PipelineRunner(resolvedMiddlewares, context, cancellationToken);
        await runner.NextAsync();
    }

    private sealed class PipelineRunner(
        ICallbackMiddleware[] middlewares,
        CallbackContext context,
        CancellationToken cancellationToken)
    {
        private int _index;

        public Task NextAsync()
        {
            if (_index < middlewares.Length)
            {
                var middleware = middlewares[_index++];
                return middleware.InvokeAsync(context, NextAsync, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}
