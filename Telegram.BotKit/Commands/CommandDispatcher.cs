using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;

namespace Telegram.BotKit.Commands;

/// <summary>
/// Dispatches command messages by executing them through the configured command middleware pipeline.
/// </summary>
internal sealed class CommandDispatcher(
    IServiceProvider serviceProvider,
    CommandPipelineBuilder pipelineBuilder
    ) : ICommandDispatcher
{
    public async Task DispatchAsync(Message message, CancellationToken cancellationToken = default)
    {
        var context = new CommandContext(message);

        using var scope = serviceProvider.CreateScope();

        // resolve middlewares from DI dynamically based on the builder configuration order
        var steps = pipelineBuilder.Steps;
        var resolvedMiddlewares = new ICommandMiddleware[steps.Count];

        for (var i = 0; i < steps.Count; i++)
        {
            resolvedMiddlewares[i] = (ICommandMiddleware)scope.ServiceProvider.GetRequiredService(steps[i].MiddlewareType);
        }

        var runner = new PipelineRunner(resolvedMiddlewares, context, cancellationToken);
        await runner.NextAsync();
    }

    private sealed class PipelineRunner(
        ICommandMiddleware[] middlewares,
        CommandContext context,
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

