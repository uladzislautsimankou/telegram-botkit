using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;

namespace Telegram.BotKit.ChosenInlineResults.Models;

/// <summary>
/// Represents the context of an incoming chosen inline result update within the processing pipeline.
/// </summary>
public sealed class ChosenInlineResultContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChosenInlineResultContext"/> class.
    /// Ensures that the incoming chosen inline result instance is not null before processing.
    /// </summary>
    /// <param name="result">The chosen inline result received from the Telegram Bot API.</param>
    /// <exception cref="ArgumentNullException">Thrown when the incoming result parameter is null.</exception>
    public ChosenInlineResultContext(ChosenInlineResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Result = result;
    }

    /// <summary>
    /// Gets the original chosen inline result received from the Telegram Bot API.
    /// </summary>
    public ChosenInlineResult Result { get; }

    /// <summary>
    /// Gets or sets the matched handler metadata containing details about the handler instance and its resolved type.
    /// </summary>
    public ChosenInlineResultMetadata? MatchedChosenInlineResultMetadata { get; set; }

    /// <summary>
    /// Gets or sets the matched handler instance assigned during the routing phase.
    /// </summary>
    internal IChosenInlineResultHandler? MatchedHandler { get; set; }
}