using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Attributes;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Tests.Callbacks.CallbackParameterBinderTests;

public sealed partial class CallbackParameterBinderTests
{
    private record TestCallbackParams
    {
        public int Id { get; init; }

        [CallbackParam(Name = "s")]
        public string? Status { get; init; }

        public TestEnum TestEnum { get; init; }

        [CallbackParam(Name = "a")]
        public bool? IsActive { get; init; }

        public Guid? Uid { get; init; }

        public DateTime? Date { get; init; }
    }

    private record RequiredCallbackParams
    {
        [CallbackParam(Required = true)]
        public int Id { get; init; }
    }

    private record DuplicateNameCallbackParams
    {
        [CallbackParam(Name = "x")]
        public int A { get; init; }

        [CallbackParam(Name = "X")]
        public int B { get; init; }
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
