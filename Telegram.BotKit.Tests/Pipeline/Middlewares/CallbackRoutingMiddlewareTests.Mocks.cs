using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public partial class CallbackRoutingMiddlewareTests
{
    private class MockCallbackHandler(string key) : ICallbackHandler<object>
    {
        public string Key { get; } = key;

        public bool WasExecuted { get; private set; }

        public Task HandleAsync(object parameters, CallbackContext context, CancellationToken cancellationToken = default)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }
    }

    private class MockCallbackParameterBinder : ICallbackParameterBinder
    {
        public TParams Bind<TParams>(CallbackContext context) where TParams : class, new() => new();
    }

    private CallbackContext CreateCallbackContext(string data)
    {
        var query = new CallbackQuery
        {
            Id = "1",
            Data = data,
            From = new User { Id = 123, FirstName = "User" },
            Message = new Message { Chat = new Chat { Id = 456 } }
        };

        return new CallbackContext(query);
    }
}
