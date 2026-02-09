using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public partial class InlineQueryRoutingMiddlewareTests
{
    private class MockInlineQueryHandler(string key) : IInlineQueryHandler<object>
    {
        public string Key { get; } = key;

        public bool WasExecuted { get; private set; }

        public Task HandleAsync(object parameters, InlineQueryContext context, CancellationToken cancellationToken = default)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }
    }

    private class MockInlineQueryParameterBinder : IInlineQueryParameterBinder
    {
        public TParams Bind<TParams>(InlineQueryContext context) where TParams : class, new() => new();
    }

    private InlineQueryContext CreateInlineContext(string queryText)
    {
        var query = new InlineQuery
        {
            Id = "1",
            Query = queryText,
            From = new User { Id = 123, FirstName = "User" }
        };

        return new InlineQueryContext(query);
    }
}
