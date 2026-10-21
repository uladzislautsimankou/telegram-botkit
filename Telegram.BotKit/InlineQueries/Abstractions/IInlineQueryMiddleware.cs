using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.InlineQueries.Abstractions;

/// <summary>
/// Represents a middleware component that processes inline queries in the pipeline.
/// </summary>
public interface IInlineQueryMiddleware : IPipelineMiddleware<InlineQueryContext>;
