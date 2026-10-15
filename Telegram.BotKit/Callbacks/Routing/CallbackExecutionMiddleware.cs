using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Callbacks.Routing;

/// <summary>
/// Execution middleware responsible for running the matched callback handler.
/// </summary>
internal sealed class CallbackExecutionMiddleware(IServiceProvider serviceProvider) : ICallbackMiddleware
{
    public ValueTask InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context is not { MatchedRegistration: not null, MatchedCallbackMetadata: not null })
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(context.MatchedRegistration.Invoker(
            context.MatchedCallbackMetadata.HandlerInstance,
            context,
            serviceProvider,
            cancellationToken));
    }
}
