using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.Example.Handlers.InlineQueries;

/// <summary>
/// Processes inline query keywords starting with "book" and supplies simulated search results.
/// </summary>
internal sealed class SearchInlineQueryHandler : IInlineQueryHandler
{
    public bool CanHandle(InlineQuery query) =>
        query.Query.StartsWith("book", StringComparison.OrdinalIgnoreCase);

    public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken)
    {
        var results = new List<InlineQueryResult>
        {
            new InlineQueryResultArticle(
                id: "book_1",
                title: "CLR via C#",
                inputMessageContent: new InputTextMessageContent("Jeffrey Richter - CLR via C#")
            )
            {
                Description = "Jeffrey Richter - CLR via C#"
            }
        };

        return Task.FromResult(new InlineQueryBatch(results, cacheTime: 300));
    }
}