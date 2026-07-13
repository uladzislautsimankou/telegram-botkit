using Telegram.BotKit.Commands.Models;

namespace Telegram.BotKit.Commands.Invocation;

/// <summary>
/// Defines an invoker contract for executing a matched command handler.
/// </summary>
internal interface ICommandHandlerInvoker
{
    /// <summary>
    /// Gets the primary trigger command name.
    /// </summary>
    string Command { get; }

    /// <summary>
    /// Gets alternative command names (aliases).
    /// </summary>
    string[] Aliases { get; }

    /// <summary>
    /// Gets the handler interface type.
    /// </summary>
    Type HandlerType { get; }

    /// <summary>
    /// Gets the handler class instance.
    /// </summary>
    object HandlerInstance { get; }

    /// <summary>
    /// Invokes the underlying command handler execution flow.
    /// </summary>
    Task InvokeAsync(CommandContext context, CancellationToken cancellationToken = default);
}
