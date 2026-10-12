namespace Telegram.BotKit.Commands.Models;

/// <summary>
/// Represents the evaluated result of routing an incoming command through the routing middleware.
/// </summary>
public readonly record struct CommandRouteResult
{
    /// <summary>
    /// Gets the status of the command routing evaluation.
    /// </summary>
    public CommandRouteStatus Status { get; init; }

    /// <summary>
    /// Gets the collection of suggested alternative commands when <see cref="Status"/> is <see cref="CommandRouteStatus.Suggestions"/>.
    /// </summary>
    public IReadOnlyList<string> Suggestions { get; init; }

    /// <summary>
    /// Creates a successful command route result.
    /// </summary>
    public static CommandRouteResult Success() => new() { Status = CommandRouteStatus.Success, Suggestions = [] };

    /// <summary>
    /// Creates a route result indicating that no matching handler was found.
    /// </summary>
    public static CommandRouteResult NotFound() => new() { Status = CommandRouteStatus.NotFound, Suggestions = [] };

    /// <summary>
    /// Creates a route result indicating that the command update should be ignored silently.
    /// </summary>
    public static CommandRouteResult Ignored() => new() { Status = CommandRouteStatus.Ignored, Suggestions = [] };

    /// <summary>
    /// Creates a route result indicating that a virtual route failed to resolve.
    /// </summary>
    public static CommandRouteResult VirtualRouteNotFound() => new() 
    { 
        Status = CommandRouteStatus.VirtualRouteNotFound,
        Suggestions = []
    };

    /// <summary>
    /// Creates a route result containing spelling suggestions for a misspelled command.
    /// </summary>
    /// <param name="suggestions">The list of similar command triggers.</param>
    public static CommandRouteResult SuggestionsFound(IReadOnlyList<string> suggestions) => new()
    {
        Status = CommandRouteStatus.Suggestions,
        Suggestions = suggestions
    };
}