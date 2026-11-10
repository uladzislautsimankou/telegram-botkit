using Telegram.Bot.Types;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.Tests.WidgetQueries;

/// <summary>
/// Builds widget queries of every kind for the tests. The properties of the Telegram.Bot types are not required,
/// so every query gets a type (plain data unless the test asks for another one) as the Bot API always sends it.
/// </summary>
internal static class WidgetQueryTestData
{
    // a tap in an original message of a chat
    public static WidgetQuery Original(
        string data,
        string id = "query_1",
        string? widgetId = null,
        WidgetQueryType? type = null) => new()
        {
            Id = id,
            From = new User { Id = 456, FirstName = "User" },
            Chat = new Chat { Id = 789 },
            MessageId = 123,
            ChatInstance = "chat_instance_1",
            Type = type ?? new WidgetQueryTypeData(),
            Data = data,
            WidgetId = widgetId
        };

    // a tap in a message sent via the bot: it has an inline message id instead of a chat and a message id
    public static WidgetQuery ViaBot(
        string data,
        string id = "query_1",
        string? widgetId = null,
        WidgetQueryType? type = null) => new()
        {
            Id = id,
            From = new User { Id = 456, FirstName = "User" },
            InlineMessageId = "inline_1",
            ChatInstance = "chat_instance_1",
            Type = type ?? new WidgetQueryTypeData(),
            Data = data,
            WidgetId = widgetId
        };

    // a tap in a forwarded copy of a shared widget: the bot learns nothing about the chat
    public static SharedWidgetQuery Copy(
        string data,
        string id = "query_1",
        string widgetId = "widget_1",
        WidgetQueryType? type = null) => new()
        {
            Id = id,
            From = new User { Id = 456, FirstName = "User" },
            Type = type ?? new WidgetQueryTypeData(),
            Data = data,
            WidgetId = widgetId
        };

    public static WidgetQueryContext Context(string data) => new(Original(data));
}
