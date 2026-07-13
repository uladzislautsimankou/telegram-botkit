using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;

namespace Telegram.BotKit.Example.Handlers.IInlineQueries;

internal sealed class ArticleInlineQueryHandler : IInlineQueryHandler
{
    public bool CanHandle(InlineQuery query) =>
        query.Query.Contains("art", StringComparison.OrdinalIgnoreCase);

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

        // Запрос содержит чувствительные данные юзера, помечаем как IsPersonal
        return Task.FromResult(new InlineQueryBatch(results, IsPersonal: true, CacheTime: 60));
    }
}