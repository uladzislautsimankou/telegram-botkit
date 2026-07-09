using FluentAssertions;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Attributes;
using Telegram.BotKit.Binding.Binders;
using Telegram.BotKit.Binding.Converters;
using Telegram.BotKit.Exceptions;

namespace Telegram.BotKit.Tests.Binding.Binders.InlineQueryParameterBinderTests;

public sealed class InlineQueryParameterBinderTests
{
    private readonly InlineQueryParameterBinder _binder;

    public InlineQueryParameterBinderTests()
    {
        var converters = new List<IValueConverter> { new DefaultValueConverter() };
        _binder = new InlineQueryParameterBinder(converters);
    }

    private record InlineDto
    {
        [InlineQyeryParam(Position = 0)]
        public string Query { get; init; } = "";

        [InlineQyeryParam(Name = "p")]
        public int Page { get; init; }

        [InlineQyeryParam(Required = true)]
        public bool IsActive { get; init; }
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

    // verifies that the inline query binder maps positional and named parameters correctly
    [Fact]
    public void Bind_WhenPositionalAndNamedParametersAreProvided_ShouldMapProperties()
    {
        // arrange
        var ctx = CreateContext("query search_term p=5 IsActive=true");

        // act
        var result = _binder.Bind<InlineDto>(ctx);

        // assert
        result.Query.Should().Be("search_term");
        result.Page.Should().Be(5);
        result.IsActive.Should().BeTrue();
    }

    // verifies that missing required inline query parameters result in a missing parameter exception
    [Fact]
    public void Bind_WhenRequiredParameterIsMissing_ShouldThrowMissingParameterException()
    {
        // arrange
        var ctx = CreateContext("query search_term p=5");

        // act
        var act = () => _binder.Bind<InlineDto>(ctx);

        // assert
        act.Should().Throw<MissingParameterException>()
            .Which.ParameterName.Should().Be("IsActive");
    }
}