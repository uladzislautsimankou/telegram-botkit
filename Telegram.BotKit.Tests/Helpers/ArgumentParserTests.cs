using FluentAssertions;
using Telegram.BotKit.Helpers;

namespace Telegram.BotKit.Tests.Helpers;

public sealed class ArgumentParserTests
{
    // verifies that standard space-separated positional arguments are parsed and extracted
    [Fact]
    public void ParseArgs_WhenPositionalArgumentsAreProvided_ShouldExtractThem()
    {
        // arrange
        var input = "one two three";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(3);
        result[0].Should().Be("one");
        result[1].Should().Be("two");
        result[2].Should().Be("three");
    }

    // verifies that positional arguments enclosed in quotes are parsed as a single argument with quotes removed
    [Fact]
    public void ParseArgs_WhenQuotedPositionalArgumentsAreProvided_ShouldExtractAndStripQuotes()
    {
        // arrange
        var input = "\"John Doe\" 25";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(2);
        result[0].Should().Be("John Doe");
        result[1].Should().Be("25");
    }

    // verifies that simple key=value named arguments are extracted correctly
    [Fact]
    public void ParseArgs_WhenNamedArgumentsAreProvided_ShouldExtractThem()
    {
        // arrange
        var input = "name=Alex age=30";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(2);
        result[0].Should().Be("name=Alex");
        result[1].Should().Be("age=30");
    }

    // verifies that named arguments with quoted values are parsed and reformatted without quotes
    [Fact]
    public void ParseArgs_WhenQuotedNamedArgumentsAreProvided_ShouldExtractAndFormat()
    {
        // arrange
        var input = "name=\"Alex Smith\" city=\"New York\"";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(2);
        result[0].Should().Be("name=Alex Smith");
        result[1].Should().Be("city=New York");
    }

    // verifies that combinations of positional, named, and quoted arguments are parsed correctly
    [Fact]
    public void ParseArgs_WhenMixedArgumentsAreProvided_ShouldExtractAllTypes()
    {
        // arrange
        var input = "\"Pos Value\" key=\"Named Value\" simple";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(3);
        result[0].Should().Be("Pos Value");
        result[1].Should().Be("key=Named Value");
        result[2].Should().Be("simple");
    }

    // verifies that multiple spaces between arguments are ignored and do not produce empty parameters
    [Fact]
    public void ParseArgs_WhenInputContainsConsecutiveSpaces_ShouldIgnoreExtraSpaces()
    {
        // arrange
        var input = "    one     \"two\"   key=\"val\"";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(3);
        result[0].Should().Be("one");
        result[1].Should().Be("two");
        result[2].Should().Be("key=val");
    }

    // verifies that empty quotes are parsed as empty strings rather than being discarded
    [Fact]
    public void ParseArgs_WhenInputContainsEmptyQuotes_ShouldExtractEmptyStrings()
    {
        // arrange
        var input = "\"\" name=\"\"";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(2);
        result[0].Should().Be(string.Empty);
        result[1].Should().Be("name=");
    }
}