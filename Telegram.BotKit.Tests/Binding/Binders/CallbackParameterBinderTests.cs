using System.Text.Json.Serialization;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;
using Telegram.BotKit.Binding.Converters;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Tests.Shared;

namespace Telegram.BotKit.Tests.Binding.Binders;

public class CallbackParameterBinderTests
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

    private readonly CallbackParameterBinder _binder;

    public CallbackParameterBinderTests()
    {
        var converters = new List<IValueConverter>
        {
            new DefaultValueConverter()
        };

        _binder = new CallbackParameterBinder(converters);
    }

    [Fact]
    public void Bind_ShouldMapStandardProperties_IgnoreCase()
    {
        // обычные параметры по имени
        var ctx = CreateContext("test?id=42");
        var result = _binder.Bind<TestCallbackParams>(ctx);
        Assert.Equal(42, result.Id);
    }

    [Fact]
    public void Bind_ShouldMapAliases()
    {
        // параметры с alias
        var ctx = CreateContext("test?s=ok&a=true");
        var result = _binder.Bind<TestCallbackParams>(ctx);

        Assert.Equal("ok", result.Status);
        Assert.True(result.IsActive);
    }

    [Theory]
    [InlineData("test?testEnum=First", TestEnum.First)]
    [InlineData("test?testEnum=second", TestEnum.Second)]
    [InlineData("test?testEnum=1", TestEnum.First)]
    public void Bind_ShouldMapEnums_VariousFormats(string data, TestEnum expected)
    {
        var ctx = CreateContext(data);
        var result = _binder.Bind<TestCallbackParams>(ctx);
        Assert.Equal(expected, result.TestEnum);
    }

    [Fact]
    public void Bind_ShouldMapGuid()
    {
        var guid = Guid.NewGuid();
        var ctx = CreateContext($"test?uid={guid}");

        var result = _binder.Bind<TestCallbackParams>(ctx);

        Assert.Equal(guid, result.Uid);
    }

    [Fact]
    public void Bind_ShouldMapDateTime()
    {
        var ctx = CreateContext("test?date=2025-12-31");

        var result = _binder.Bind<TestCallbackParams>(ctx);

        Assert.Equal(new DateTime(2025, 12, 31), result.Date);
    }

    [Fact]
    public void Bind_ShouldIgnoreUnknownParams()
    {
        var ctx = CreateContext("test?id=1&unknown=val");
        var result = _binder.Bind<TestCallbackParams>(ctx);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public void Bind_ShouldThrow_WhenTypeMismatch()
    {
        var ctx = CreateContext("test?id=not-a-number");

        var ex = Assert.Throws<InvalidParameterTypeException>(() =>
            _binder.Bind<TestCallbackParams>(ctx));

        Assert.Equal("id", ex.ParameterName);
        Assert.Equal("not-a-number", ex.RawValue);
    }

    [Fact]
    public void Bind_ShouldHandleShortAlias_InException()
    {
        var ctx = CreateContext("test?a=invalid-bool");

        var ex = Assert.Throws<InvalidParameterTypeException>(() =>
            _binder.Bind<TestCallbackParams>(ctx));

        Assert.Equal("a", ex.ParameterName);
    }
}
