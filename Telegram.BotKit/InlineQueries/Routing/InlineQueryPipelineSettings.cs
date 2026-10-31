using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.InlineQueries.Routing;

/// <summary>
/// Holds the settings of the inline query pipeline.
/// </summary>
/// <param name="ExecutionMode">The mode in which several handlers matching the same inline query are executed.</param>
internal sealed record InlineQueryPipelineSettings(InlineQueryExecutionMode ExecutionMode);
