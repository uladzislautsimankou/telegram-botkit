namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Defines a handler for a specific text command with strongly typed arguments.
/// </summary>
/// <typeparam name="TParams">The type of the model to bind the command arguments to.</typeparam>
public interface ICommandHandler<TParams> where TParams : class, new()
{
    /// <summary>
    /// Gets the command name (trigger) without the leading slash (e.g., "start" for /start).
    /// </summary>
    string Command { get; }

    /// <summary>
    /// Handles the command execution.
    /// </summary>
    /// <param name="parameters">The strongly typed arguments parsed and bound from the command string.</param>
    /// <param name="context">The context containing the message and metadata.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task HandleAsync(TParams parameters, CommandContext context, CancellationToken cancellationToken = default);
}