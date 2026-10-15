using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Commands;

/// <summary>
/// Dispatches command messages by executing them through the compiled command middleware pipeline.
/// </summary>
internal sealed class CommandDispatcher(
    IServiceProvider serviceProvider,
    PipelineDelegate<CommandContext> pipeline) : ICommandDispatcher
{
    public async Task DispatchAsync(Message message, CancellationToken cancellationToken = default)
    {
        var context = new CommandContext(message);

        // create a dedicated async dependency scope for the incoming command execution
        await using var scope = serviceProvider.CreateAsyncScope();

        // execute the precompiled middleware delegate chain
        await pipeline(context, scope.ServiceProvider, cancellationToken);
    }
}
