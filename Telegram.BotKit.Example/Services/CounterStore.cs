using System.Collections.Concurrent;

namespace Telegram.BotKit.Example.Services;

/// <summary>
/// Keeps the counter of every message with the counter widget. A widget query tells the bot which message was tapped,
/// but not what the widget shows, so the bot has to remember it. In memory only: the counters are lost on restart
/// (the widget shows the last state the bot wrote to the message); a real bot keeps them in a database.
/// </summary>
internal sealed class CounterStore
{
    private readonly ConcurrentDictionary<(long ChatId, int MessageId), int> _counters = new();

    public int Add(long chatId, int messageId, int by) =>
        _counters.AddOrUpdate((chatId, messageId), by, (_, current) => current + by);

    /// <summary>
    /// The chat state of the widget: a text of up to 1024 bytes that only the bot writes.
    /// Here it is JSON that the script of the widget parses.
    /// </summary>
    public static string ToState(int total) => $"{{\"n\":{total}}}";
}
