using Telegram.BotKit.InlineQueries.Abstractions;

namespace Telegram.BotKit.InlineQueries.Middlewares;

/// <summary>
/// Represents a middleware responsible for sending aggregated inline query results to Telegram.
/// </summary>
public interface IInlineQueryAnswerMiddleware : IInlineQueryMiddleware
{
}