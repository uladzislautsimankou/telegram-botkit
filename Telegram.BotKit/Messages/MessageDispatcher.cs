using Telegram.Bot.Types;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Messages;

/// <summary>
/// Dispatches incoming message updates by executing them through the compiled middleware pipeline.
/// </summary>
/// <param name="serviceProvider">The root service provider used to create a dependency scope for each update.</param>
/// <param name="pipeline">The compiled message middleware pipeline.</param>
internal sealed class MessageDispatcher(IServiceProvider serviceProvider, PipelineDelegate<MessageContext> pipeline)
    : DispatcherBase<MessageContext>(serviceProvider, pipeline), IMessageDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(Message message, CancellationToken cancellationToken = default) =>
        RunAsync(new MessageContext(message), cancellationToken);
}
