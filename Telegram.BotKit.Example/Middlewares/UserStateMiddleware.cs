using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Example.Middlewares;

/// <summary>
/// Demonstrates dialog state enrichment on message contexts.
/// </summary>
internal sealed class UserStateMiddleware : IMessageMiddleware
{
    public async ValueTask InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        // mock state assignment based on input prefix
        if (context.Message.Text?.StartsWith("feedback:", StringComparison.OrdinalIgnoreCase) is true)
        {
            context.UserState = "awaiting_feedback";
        }

        await next();
    }
}