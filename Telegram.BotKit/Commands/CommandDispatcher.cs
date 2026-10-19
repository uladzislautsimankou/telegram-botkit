using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Commands;

/// <summary>
/// Dispatches command messages by executing them through the compiled middleware pipeline.
/// </summary>
/// <param name="serviceProvider">The root service provider used to create a dependency scope for each update.</param>
/// <param name="pipeline">The compiled command middleware pipeline.</param>
internal sealed class CommandDispatcher(IServiceProvider serviceProvider, PipelineDelegate<CommandContext> pipeline)
    : DispatcherBase<CommandContext>(serviceProvider, pipeline), ICommandDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(Message message, CancellationToken cancellationToken = default) =>
        RunAsync(new CommandContext(message), cancellationToken);
}
