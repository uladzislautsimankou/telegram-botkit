using FluentAssertions;
using Telegram.BotKit.Binding.Parsers;

namespace Telegram.BotKit.Tests.Binding.Parsers;

public sealed class CallbackParserTests
{
    // verifies that passing null returns an empty command key and empty parameters
    [Fact]
    public void Parse_WhenInputIsNull_ShouldReturnEmptyKeyAndEmptyParameters()
    {
        // arrange
        string? input = null;

        // act
        var result = CallbackParser.Parse(input);

        // assert
        result.Key.Should().BeEmpty();
        result.Parameters.Should().BeEmpty();
    }

    // verifies that callback query strings are correctly split into their trigger key and raw parameters
    [Theory]
    [InlineData("menu", "menu", 0)]                     // Only key
    [InlineData("game:start?id=123", "game:start", 1)]  // Key + 1 parameter
    [InlineData("nav?to=settings&b=main", "nav", 2)]    // Key + 2 parameters
    public void Parse_WhenInputContainsParameters_ShouldExtractKeyAndCorrectParametersCount(string input, string expectedKey, int expectedCount)
    {
        // arrange & act
        var result = CallbackParser.Parse(input);

        // assert
        result.Key.Should().Be(expectedKey);
        result.Parameters.Should().HaveCount(expectedCount);
    }

    // verifies that parameter names and their raw string values are mapped correctly
    [Fact]
    public void Parse_WhenQueryHasMultipleParameters_ShouldExtractCorrectKeyValues()
    {
        // arrange
        var input = "vote?id=5&opt=yes";

        // act
        var result = CallbackParser.Parse(input);

        // assert
        result.Parameters.Should().ContainKey("id").WhoseValue.Should().Be("5");
        result.Parameters.Should().ContainKey("opt").WhoseValue.Should().Be("yes");
    }

    // verifies that parameter dictionary searches are case-insensitive
    [Fact]
    public void Parse_WhenParameterKeysHaveDifferentCasing_ShouldBeCaseInsensitive()
    {
        // arrange
        var input = "test?ID=123";

        // act
        var result = CallbackParser.Parse(input);

        // assert
        result.Parameters.Should().ContainKey("id").WhoseValue.Should().Be("123");
    }

    // verifies that query keys without explicitly assigned values are parsed with empty strings as values
    [Fact]
    public void Parse_WhenQueryContainsEmptyValues_ShouldExtractKeyWithEmptyStringValue()
    {
        // arrange
        var input = "filter?active=&id=1";

        // act
        var result = CallbackParser.Parse(input);

        // assert
        result.Parameters.Should().ContainKey("active").WhoseValue.Should().BeEmpty();
        result.Parameters.Should().ContainKey("id").WhoseValue.Should().Be("1");
    }

    // verifies that callback data without a query separator is parsed as a standalone action key
    [Fact]
    public void Parse_WhenQueryHasNoQuerySeparator_ShouldTreatEntireStringAsKey()
    {
        // arrange
        var input = "simple_action";

        // act
        var result = CallbackParser.Parse(input);

        // assert
        result.Key.Should().Be("simple_action");
        result.Parameters.Should().BeEmpty();
    }

    // verifies that passing whitespace-only data returns an empty command key and empty parameters
    [Fact]
    public void Parse_WhenInputIsWhitespace_ShouldReturnEmptyKeyAndEmptyParameters()
    {
        // arrange
        var input = "   ";

        // act
        var result = CallbackParser.Parse(input);

        // assert
        result.Key.Should().BeEmpty();
        result.Parameters.Should().BeEmpty();
    }
}