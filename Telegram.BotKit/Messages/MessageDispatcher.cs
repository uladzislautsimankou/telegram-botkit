using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Messages;

/// <summary>
/// Dispatches incoming message updates by executing them through the compiled message middleware pipeline.
/// </summary>
internal sealed class MessageDispatcher(
    IServiceProvider serviceProvider,
    PipelineDelegate<MessageContext> pipeline) : IMessageDispatcher
{
    public async Task DispatchAsync(Message message, CancellationToken cancellationToken = default)
    {
        var context = new MessageContext(message);

        // create a dedicated async dependency scope for the incoming message execution
        await using var scope = serviceProvider.CreateAsyncScope();

        // execute the precompiled middleware delegate chain
        await pipeline(context, scope.ServiceProvider, cancellationToken);
    }
}
