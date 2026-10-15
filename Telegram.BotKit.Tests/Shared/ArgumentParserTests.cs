using FluentAssertions;
using Telegram.BotKit.Helpers;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Shared;

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

    // verifies that null, empty, or whitespace-only inputs return an empty list without throwing
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \t \r\n  ")]
    public void ParseArgs_WhenInputIsNullOrWhiteSpace_ShouldReturnEmptyList(string? input)
    {
        // arrange
        var rawInput = input;

        // act
        var result = ArgumentParser.ParseArgs(rawInput!);

        // assert
        result.Should().BeEmpty();
    }

    // verifies that typographical and mobile quotation marks are normalized and stripped correctly
    [Fact]
    public void ParseArgs_WhenTypographicalQuotesAreProvided_ShouldExtractAndNormalizeThem()
    {
        // arrange
        var input = "«first value» “second value” key=„third value“";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(3);
        result[0].Should().Be("first value");
        result[1].Should().Be("second value");
        result[2].Should().Be("key=third value");
    }

    // verifies that an unclosed quotation mark causes an InputParsingException with the original input preserved
    [Fact]
    public void ParseArgs_WhenUnclosedQuoteIsProvided_ShouldThrowInputParsingException()
    {
        // arrange
        var input = "valid \"unclosed value";

        // act
        var act = () => ArgumentParser.ParseArgs(input);

        // assert
        act.Should().Throw<InputParsingException>()
            .Which.Input.Should().Be(input);
    }

    // verifies that an unclosed typographical quotation mark causes an InputParsingException
    [Theory]
    [InlineData("«unclosed value")]
    [InlineData("“unclosed value")]
    [InlineData("„unclosed value")]
    public void ParseArgs_WhenUnclosedTypographicalQuoteIsProvided_ShouldThrowInputParsingException(string input)
    {
        // arrange
        var rawInput = input;

        // act
        var act = () => ArgumentParser.ParseArgs(rawInput);

        // assert
        act.Should().Throw<InputParsingException>()
            .Which.Input.Should().Be(input);
    }

    // verifies that escaped quotation marks inside a quoted string are preserved as literal characters
    [Fact]
    public void ParseArgs_WhenEscapedQuotesAreProvided_ShouldPreserveEscapedQuoteCharacter()
    {
        // arrange
        var input = "\"He said \\\"Hello\\\"\" key=\"val=\\\"123\\\"\"";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(2);
        result[0].Should().Be("He said \"Hello\"");
        result[1].Should().Be("key=val=\"123\"");
    }

    // verifies that escaped backslashes are resolved into literal backslashes and trailing backslashes are preserved
    [Fact]
    public void ParseArgs_WhenEscapedAndTrailingBackslashesAreProvided_ShouldHandleThemProperly()
    {
        // arrange
        var input = "path\\\\to\\\\file folder\\";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(2);
        result[0].Should().Be("path\\to\\file");
        result[1].Should().Be("folder\\");
    }

    // verifies that tab characters and line breaks are treated as valid whitespace separators
    [Fact]
    public void ParseArgs_WhenInputContainsTabsAndLineBreaks_ShouldSplitTokensProperly()
    {
        // arrange
        var input = "one\ttwo\r\n\"three four\"\nkey=val";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(4);
        result[0].Should().Be("one");
        result[1].Should().Be("two");
        result[2].Should().Be("three four");
        result[3].Should().Be("key=val");
    }

    // verifies that a trailing backslash at the very end of the input is preserved as a literal backslash
    [Fact]
    public void ParseArgs_WhenTrailingBackslashIsAtEndOfInput_ShouldPreserveIt()
    {
        // arrange
        var input = "folder\\";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().ContainSingle().Which.Should().Be("folder\\");
    }

    // verifies that an input consisting solely of a backslash yields a single literal backslash
    [Fact]
    public void ParseArgs_WhenInputIsOnlyBackslash_ShouldReturnSingleBackslash()
    {
        // arrange
        var input = "\\";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().ContainSingle().Which.Should().Be("\\");
    }

    // verifies that a token starting with an escaped quote is parsed correctly
    [Fact]
    public void ParseArgs_WhenTokenStartsWithEscape_ShouldParseCorrectly()
    {
        // arrange
        var input = "\\\"quoted\\\"";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().ContainSingle().Which.Should().Be("\"quoted\"");
    }

    // verifies that an escaped whitespace character does not split the token
    [Fact]
    public void ParseArgs_WhenSpaceIsEscaped_ShouldNotSplitToken()
    {
        // arrange
        var input = "hello\\ world";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().ContainSingle().Which.Should().Be("hello world");
    }

    // verifies that an escaped backslash outside quotes resolves to a single literal backslash
    [Fact]
    public void ParseArgs_WhenBackslashIsEscapedOutsideQuotes_ShouldPreserveLiteralBackslash()
    {
        // arrange
        var input = "a\\\\b";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().ContainSingle().Which.Should().Be("a\\b");
    }

    // verifies that a quote appearing mid-token toggles quoting without splitting the token
    [Fact]
    public void ParseArgs_WhenQuoteAppearsMidToken_ShouldToggleQuoting()
    {
        // arrange
        var input = "ab\"cd ef\"gh";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().ContainSingle().Which.Should().Be("abcd efgh");
    }

    // verifies that adjacent quoted and unquoted segments merge into a single token
    [Fact]
    public void ParseArgs_WhenQuotesAreAdjacentToText_ShouldMergeIntoOneToken()
    {
        // arrange
        var input = "a\"b\"c \"d\"e";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(2);
        result[0].Should().Be("abc");
        result[1].Should().Be("de");
    }

    // verifies that a token consisting only of typographic quotes yields an empty string
    [Fact]
    public void ParseArgs_WhenTokenIsOnlyTypographicQuotes_ShouldReturnEmptyString()
    {
        // arrange
        var input = "«»";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().ContainSingle().Which.Should().Be(string.Empty);
    }

    // verifies that a quoted token longer than the stackalloc threshold is parsed correctly
    [Fact]
    public void ParseArgs_WhenTokenExceedsStackallocThreshold_ShouldParseCorrectly()
    {
        // arrange
        var longValue = new string('x', 200);
        var input = $"key=\"{longValue}\"";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().ContainSingle().Which.Should().Be($"key={longValue}");
    }

    // verifies that a complex token at the beginning of a long input does not break parsing
    [Fact]
    public void ParseArgs_WhenComplexTokenStartsAtBeginningOfLongInput_ShouldNotThrow()
    {
        // arrange
        var input = "\"a\" " + new string('b', 5000);

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(2);
        result[0].Should().Be("a");
        result[1].Should().Be(new string('b', 5000));
    }

    // verifies that an empty quoted argument at the end of the input is preserved as an empty string
    [Fact]
    public void ParseArgs_WhenInputEndsWithQuotedEmpty_ShouldPreserveIt()
    {
        // arrange
        var input = "a \"\"";

        // act
        var result = ArgumentParser.ParseArgs(input);

        // assert
        result.Should().HaveCount(2);
        result[0].Should().Be("a");
        result[1].Should().Be(string.Empty);
    }

    // verifies that every typographic quote character is normalized and stripped correctly
    [Theory]
    [InlineData("«value»")]
    [InlineData("“value”")]
    [InlineData("„value“")]
    [InlineData("„value”")]
    [InlineData("“value„")]
    public void ParseArgs_WhenEachTypographicQuoteIsUsed_ShouldNormalizeAndStrip(string input)
    {
        // arrange
        var rawInput = input;

        // act
        var result = ArgumentParser.ParseArgs(rawInput);

        // assert
        result.Should().ContainSingle().Which.Should().Be("value");
    }

    // verifies that the original input is preserved in the exception even when the unclosed quote follows escaped content
    [Fact]
    public void ParseArgs_WhenUnclosedQuoteFollowsEscapedContent_ShouldPreserveOriginalInput()
    {
        // arrange
        var input = "a\\\\b \"unclosed";

        // act
        var act = () => ArgumentParser.ParseArgs(input);

        // assert
        act.Should().Throw<InputParsingException>()
            .Which.Input.Should().Be(input);
    }
}
