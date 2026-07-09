using System.Text.Json.Serialization;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;
using Telegram.BotKit.Binding.Converters;
using Telegram.BotKit.Tests.Shared;

namespace Telegram.BotKit.Tests.Binding.Binders.CallbackParameterBinderTests;

public sealed partial class CallbackParameterBinderTests
{
    private record TestCallbackParams
    {
        public int Id { get; init; }

        [JsonPropertyName("s")]
        public string? Status { get; init; }

        public TestEnum TestEnum { get; init; }

        [JsonPropertyName("a")]
        public bool? IsActive { get; init; }

        public Guid? Uid { get; init; }

        public DateTime? Date { get; init; }
    }

    private readonly CallbackParameterBinder _binder;

    public CallbackParameterBinderTests()
    {
        var converters = new List<IValueConverter>
        {
            new DefaultValueConverter()
        };

        _binder = new CallbackParameterBinder(converters);
    }

    private CallbackContext CreateContext(string data)
    {
        var query = new CallbackQuery
        {
            Data = data,
            Message = new Message { Id = 123, Chat = new Chat { Id = 1 } },
            From = new User { Id = 999, FirstName = "TestUser" }
        };

        return new CallbackContext(query);
    }
}