using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Attributes;
using Telegram.BotKit.Example.Constants;

namespace Telegram.BotKit.Example.Handlers.IInlineQueries;

internal record SearchQueryParams
{
    // required query parameter. Example: @YourBot search test_query
    [InlineQyeryParam(Position = 0, Required = true)]
    public string Query { get; init; } = string.Empty;

    // optional named argument limiting results. Example: @YourBot search test_query limit=10
    [InlineQyeryParam(Name = "limit")]
    public int Limit { get; init; } = 5;
}

/// <summary>
/// Processes inline query keywords starting with "search" and supplies simulated search results.
/// </summary>
internal sealed class SearchInlineQueryHandler(ITelegramBotClient bot) : IInlineQueryHandler<SearchQueryParams>
{
    public string Key => InlineQueryKeys.Example.Search;

    public async Task HandleAsync(SearchQueryParams parameters, InlineQueryContext context, CancellationToken cancellationToken = default)
    {
        var results = new InlineQueryResult[]
        {
            new InlineQueryResultArticle(
                id: "1",
                title: $"Result for: {parameters.Query}",
                inputMessageContent: new InputTextMessageContent($"Selected: {parameters.Query} (Max limit: {parameters.Limit})")
            )
        };

        await bot.AnswerInlineQuery(
          context.Id,
            results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken
        );
    }
}