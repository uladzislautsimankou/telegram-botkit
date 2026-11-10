using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.WidgetQueries.Abstractions;

/// <summary>
/// Represents a middleware component that processes widget queries in the pipeline.
/// </summary>
public interface IWidgetQueryMiddleware : IPipelineMiddleware<WidgetQueryContext>;
