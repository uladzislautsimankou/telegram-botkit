using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Attributes;
using Telegram.BotKit.Binding.Binders;
using Telegram.BotKit.Binding.Converters;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Tests.Binding.Binders;

public class InlineQueryParameterBinderTests
{
    private record InlineDto
    {
        [InlineQyeryParam(Position = 0)]
        public string Query { get; init; } = "";

        [InlineQyeryParam(Name = "p")]
        public int Page { get; init; }

        [InlineQyeryParam(Required = true)]
        public bool IsActive { get; init; }
    }

    private readonly InlineQueryParameterBinder _binder;

    public InlineQueryParameterBinderTests()
    {
        var converters = new List<IValueConverter> { new DefaultValueConverter() };
        _binder = new InlineQueryParameterBinder(converters);
    }

    private InlineQueryContext CreateContext(string queryText)
    {
        var inlineQuery = new InlineQuery
        {
            Id = "1",
            Query = queryText,
            From = new User { Id = 1, FirstName = "User" }
        };

        return new InlineQueryContext(inlineQuery);
    }

    [Fact]
    public void Bind_ShouldMapPositionalAndNamed()
    {
        // Проверяем, что атрибут InlineParam корректно передал Position=0 и Name="p" в базовый класс
        // queryText: "search_term p=5 IsActive=true"
        var ctx = CreateContext("query search_term p=5 IsActive=true");

        var result = _binder.Bind<InlineDto>(ctx);

        Assert.Equal("search_term", result.Query);
        Assert.Equal(5, result.Page);
        Assert.True(result.IsActive);
    }

    [Fact]
    public void Bind_ShouldThrow_WhenMissingRequired()
    {
        // Проверяем, что атрибут InlineParam корректно передал Required=true
        var ctx = CreateContext("query search_term p=5"); // Забыли IsActive

        var ex = Assert.Throws<MissingParameterException>(() =>
            _binder.Bind<InlineDto>(ctx));

        Assert.Equal("IsActive", ex.ParameterName);
    }
}
