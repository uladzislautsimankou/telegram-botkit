namespace Telegram.BotKit.Commands.Models;

/// <summary>
/// Specifies the outcome status of matching an incoming command update against registered routes.
/// </summary>
public enum CommandRouteStatus
{
    /// <summary>
    /// Initial or unassigned routing status.
    /// </summary>
    None = 0,

    /// <summary>
    /// A matching command handler or alias was successfully located and assigned.
    /// </summary>
    Success,

    /// <summary>
    /// No matching command handler was found for the incoming command trigger.
    /// </summary>
    NotFound,

    /// <summary>
    /// The incoming command was not matched, but close spelling suggestions were identified.
    /// </summary>
    Suggestions,

    /// <summary>
    /// A virtual route override was specified but did not match any registered handler.
    /// </summary>
    VirtualRouteNotFound,

    /// <summary>
    /// The update was ignored due to channel targeting, chat filters, or targeting a different bot.
    /// </summary>
    Ignored
}