using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Example.Handlers.IInlineQueries;

internal record RawQueryParams
{
    public string? RawText { get; init; }
}

internal sealed class InteractiveInlineQueryHandler(ITelegramBotClient bot) : IInlineQueryHandler<RawQueryParams>
{
    // empty key captures all raw input that doesn't match specific query keys
    public string Key => string.Empty;

    public async Task HandleAsync(RawQueryParams parameters, InlineQueryContext context, CancellationToken cancellationToken)
    {
        var input = context.Query?.Trim() ?? string.Empty;
        var results = new List<InlineQueryResult>();

        var promoCodeText = string.IsNullOrWhiteSpace(input) ? "promocode" : input;
        results.Add(new InlineQueryResultArticle(
            id: "promo",
            title: "Enter Promo Code",
            inputMessageContent: new InputTextMessageContent($"Promo code applied: {promoCodeText}")
        ));

        // conditionally show another result if input can be parsed as a number
        var isNumeric = int.TryParse(input, out var number);
        if (string.IsNullOrWhiteSpace(input) || isNumeric)
        {
            results.Add(new InlineQueryResultArticle(
                id: "add_number",
                title: "Add Number",
                inputMessageContent: new InputTextMessageContent($"Number added: {(isNumeric ? number : 0)}")
            ));
        }

        await bot.AnswerInlineQuery(context.Id, results, cancellationToken: cancellationToken);
    }
}