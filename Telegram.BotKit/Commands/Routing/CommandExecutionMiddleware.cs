using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Commands.Routing;

/// <summary>
/// Execution middleware responsible for running the matched command handler.
/// </summary>
internal sealed class CommandExecutionMiddleware(IServiceProvider serviceProvider) : ICommandMiddleware
{
    public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context is not { MatchedRegistration: not null, MatchedCommandMetadata: not null })
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(context.MatchedRegistration.Invoker(
            context.MatchedCommandMetadata.HandlerInstance,
            context,
            serviceProvider,
            cancellationToken));
    }
}
