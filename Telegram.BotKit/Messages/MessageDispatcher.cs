using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;

namespace Telegram.BotKit.Messages;

/// <summary>
/// Dispatches incoming message updates by executing them through the configured message middleware pipeline.
/// </summary>
internal sealed class MessageDispatcher(
    IServiceProvider serviceProvider,
    MessagePipelineBuilder pipelineBuilder
    ) : IMessageDispatcher
{
    public async Task DispatchAsync(Message message, CancellationToken cancellationToken = default)
    {
        var context = new MessageContext(message);

        using var scope = serviceProvider.CreateScope();

        // resolve middlewares from DI dynamically based on the builder configuration order
        var steps = pipelineBuilder.Steps;
        var resolvedMiddlewares = new IMessageMiddleware[steps.Count];

        for (var i = 0; i < steps.Count; i++)
        {
            resolvedMiddlewares[i] = (IMessageMiddleware)scope.ServiceProvider.GetRequiredService(steps[i].MiddlewareType);
        }

        var runner = new PipelineRunner(resolvedMiddlewares, context, cancellationToken);
        await runner.NextAsync();
    }

    private sealed class PipelineRunner(
        IMessageMiddleware[] middlewares,
        MessageContext context,
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