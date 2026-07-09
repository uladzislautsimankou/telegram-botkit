using FluentAssertions;
using Telegram.BotKit.Binding.Parsers;

namespace Telegram.BotKit.Tests.Binding.Parsers;

public sealed class InlineQueryParserTests
{
    // verifies that parsing a null inline query string returns empty key and parameters
    [Fact]
    public void Parse_WhenInputIsNull_ShouldReturnEmptyKeyAndParameters()
    {
        // arrange
        string? input = null;

        // act
        var result = InlineQueryParser.Parse(input);

        // assert
        result.Key.Should().BeEmpty();
        result.Parameters.Should().BeEmpty();
    }

    // verifies that a simple word query is parsed as a standalone trigger key
    [Fact]
    public void Parse_WhenQueryHasOnlyKey_ShouldExtractKeyWithoutParameters()
    {
        // arrange
        var input = "find";

        // act
        var result = InlineQueryParser.Parse(input);

        // assert
        result.Key.Should().Be("find");
    }

    // verifies that queries containing multiple arguments split the first term as the key and the rest as parameter tokens
    [Fact]
    public void Parse_WhenQueryHasArguments_ShouldExtractKeyAndParameters()
    {
        // arrange
        var input = "find one two three";

        // act
        var result = InlineQueryParser.Parse(input);

        // assert
        result.Key.Should().Be("find");
        result.Parameters.Should().HaveCount(3);
        result.Parameters[0].Should().Be("one");
        result.Parameters[1].Should().Be("two");
        result.Parameters[2].Should().Be("three");
    }

    // verifies that whitespace-only query strings are parsed as empty triggers and parameters
    [Fact]
    public void Parse_WhenInputIsWhitespace_ShouldReturnEmptyKeyAndParameters()
    {
        // arrange
        var input = "   ";

        // act
        var result = InlineQueryParser.Parse(input);

        // assert
        result.Key.Should().BeEmpty();
        result.Parameters.Should().BeEmpty();
    }
}