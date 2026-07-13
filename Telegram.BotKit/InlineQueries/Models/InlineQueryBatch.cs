using Telegram.Bot.Types.InlineQueryResults;

namespace Telegram.BotKit.InlineQueries.Models;

/// <summary>
/// Represents a batch of results returned by an individual inline query handler.
/// </summary>
public sealed record InlineQueryBatch(
    IEnumerable<InlineQueryResult> Results,
    bool IsPersonal = false,
    int? CacheTime = null,
    string? NextOffset = null);