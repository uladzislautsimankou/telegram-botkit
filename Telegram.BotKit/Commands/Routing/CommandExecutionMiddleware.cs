using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Commands.Routing;

/// <summary>
/// Execution middleware responsible for running the matched command handler.
/// </summary>
internal sealed class CommandExecutionMiddleware : ICommandMiddleware
{
    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedInvoker is not null)
        {
            await context.MatchedInvoker.InvokeAsync(context, cancellationToken);
        }

        // do not invoke next() as this is the end of the pipeline
    }
}