using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Callbacks.Routing;

/// <summary>
/// Execution middleware responsible for running the matched callback handler.
/// </summary>
internal sealed class CallbackExecutionMiddleware : ICallbackMiddleware
{
    public async Task InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedInvoker is not null)
        {
            await context.MatchedInvoker.InvokeAsync(context, cancellationToken);
        }
    }
}