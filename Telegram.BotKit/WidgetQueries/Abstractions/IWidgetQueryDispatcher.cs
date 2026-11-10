using Telegram.Bot.Types;

namespace Telegram.BotKit.WidgetQueries.Abstractions;

/// <summary>
/// Defines a mechanism for dispatching widget queries (taps on a widget that asks the bot) to the middleware pipeline.
/// </summary>
public interface IWidgetQueryDispatcher
{
    /// <summary>
    /// Processes a query from an original widget message (update <c>widget_query</c>), executes the middleware chain
    /// and invokes the matching handler.
    /// </summary>
    /// <param name="query">The widget query received from Telegram.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DispatchAsync(WidgetQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a query from a forwarded copy of a shared widget (update <c>shared_widget_query</c>), executes the
    /// middleware chain and invokes the matching handler.
    /// </summary>
    /// <param name="query">The shared widget query received from Telegram.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DispatchAsync(SharedWidgetQuery query, CancellationToken cancellationToken = default);
}
