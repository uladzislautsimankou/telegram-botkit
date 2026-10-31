using Telegram.Bot;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Placed after <c>UseRouting()</c>: reacts to the result of callback routing.
/// It runs even when no handler is registered for the callback key.
/// </summary>
internal sealed class CallbackRoutingResultMiddleware(
    ITelegramBotClient bot,
    ILogger<CallbackRoutingResultMiddleware> logger
    ) : ICallbackMiddleware
{
    public async ValueTask InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        // routing succeeded: continue to the handler
        if (context.MatchedCallbackMetadata is { } metadata)
        {
            logger.LogDebug("Callback '{Key}' is routed to {Handler}", context.Key, metadata.HandlerType.Name);
            await next();
            return;
        }

        // routing failed, typically a button of a message sent before a deployment renamed or removed its handler;
        // the framework never answers callback queries, so without this the button keeps showing the loading indicator
        await bot.AnswerCallbackQuery(
            callbackQueryId: context.Query.Id,
            text: "This button is no longer active.",
            showAlert: true,
            cancellationToken: cancellationToken);
    }
}
