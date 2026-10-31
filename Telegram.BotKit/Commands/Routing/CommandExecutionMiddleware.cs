using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Commands.Routing;

/// <summary>
/// Execution middleware responsible for running the matched command handler.
/// Contexts without a matched handler (see <see cref="CommandContext.RouteResult"/>) are skipped.
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve the parameter binder.</param>
internal sealed class CommandExecutionMiddleware(IServiceProvider serviceProvider) : ICommandMiddleware
{
    /// <inheritdoc />
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
