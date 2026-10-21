using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.Example.Handlers.IInlineQueries;

internal sealed class ArticleInlineQueryHandler : IInlineQueryHandler
{
    public static bool CanHandle(InlineQuery query) =>
        string.IsNullOrWhiteSpace(query.Query)
        || query.Query.Contains("art", StringComparison.OrdinalIgnoreCase);

    public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken)
    {
        var results = new List<InlineQueryResult>
        {
            new InlineQueryResultArticle(
                id: "article_1",
                title: "What's new in .NET 10",
                inputMessageContent: new InputTextMessageContent("Discover latest updates in .NET 10")
            )
        };

        // query contains sensitive user data, marked as personal
        return Task.FromResult(new InlineQueryBatch(results, isPersonal: true, cacheTime: 60));
    }
}
