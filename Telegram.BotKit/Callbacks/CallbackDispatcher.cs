using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Callbacks;

/// <summary>
/// Dispatches callback queries by executing them through the compiled middleware pipeline.
/// </summary>
internal sealed class CallbackDispatcher(
    IServiceProvider serviceProvider,
    PipelineDelegate<CallbackContext> pipeline) : ICallbackDispatcher
{
    public async Task DispatchAsync(CallbackQuery query, CancellationToken cancellationToken = default)
    {
        var context = new CallbackContext(query);

        // create a dedicated async dependency scope for the incoming callback request
        await using var scope = serviceProvider.CreateAsyncScope();

        // execute the precompiled middleware delegate chain
        await pipeline(context, scope.ServiceProvider, cancellationToken);
    }
}