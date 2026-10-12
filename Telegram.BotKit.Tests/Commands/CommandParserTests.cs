using FluentAssertions;
using Telegram.BotKit.Commands.Parsing;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandParserTests
{
    // verifies that passing a null command returns an empty trigger, null bot suffix, and empty raw args
    [Fact]
    public void ParseHeader_WhenInputIsNull_ShouldReturnEmptyCommandAndNullBotUsername()
    {
        // arrange
        string? input = null;

        // act
        var result = CommandParser.ParseHeader(input);

        // assert
        result.Command.Should().BeEmpty();
        result.BotUsername.Should().BeNull();
        result.RawArgs.Should().BeEmpty();
    }

    // verifies that texts without a leading slash are ignored and not treated as commands
    [Fact]
    public void ParseHeader_WhenInputDoesNotStartWithSlash_ShouldReturnEmptyCommand()
    {
        // arrange
        var input = "just text";

        // act
        var result = CommandParser.ParseHeader(input);

        // assert
        result.Command.Should().BeEmpty();
        result.RawArgs.Should().BeEmpty();
    }

    // verifies that command triggers and explicit target bot usernames are extracted correctly
    [Theory]
    [InlineData("/start", "start", null, "")]
    [InlineData("/start@MyBot", "start", "MyBot", "")]
    [InlineData("/duel@GameBot rps", "duel", "GameBot", "rps")]
    public void ParseHeader_WhenCommandWithBotUsernameSymmetric_ShouldExtractCommandAndBotUsername(
        string input,
        string expectedCmd,
        string? expectedBot,
        string expectedRawArgs)
    {
        // arrange & act
        var result = CommandParser.ParseHeader(input);

        // assert
        result.Command.Should().Be(expectedCmd);
        result.BotUsername.Should().Be(expectedBot);
        result.RawArgs.Should().Be(expectedRawArgs);
    }

    // verifies that passing an empty command string returns an empty command key and empty raw args
    [Fact]
    public void ParseHeader_WhenInputIsEmpty_ShouldReturnEmptyCommandAndNullBotUsername()
    {
        // arrange
        var input = "";

        // act
        var result = CommandParser.ParseHeader(input);

        // assert
        result.Command.Should().BeEmpty();
        result.BotUsername.Should().BeNull();
        result.RawArgs.Should().BeEmpty();
    }

    // verifies that passing only a slash with no text returns an empty command key
    [Fact]
    public void ParseHeader_WhenInputIsOnlySlash_ShouldReturnEmptyCommand()
    {
        // arrange
        var input = "/";

        // act
        var result = CommandParser.ParseHeader(input);

        // assert
        result.Command.Should().BeEmpty();
        result.RawArgs.Should().BeEmpty();
    }

    // verifies that commands followed by newlines extract the trigger and raw argument substring
    [Fact]
    public void ParseHeader_WhenCommandIsFollowedByNewline_ShouldExtractCommandAndRawArgs()
    {
        // arrange
        var input = "/broadcast\nfirst_arg second_arg";

        // act
        var result = CommandParser.ParseHeader(input);

        // assert
        result.Command.Should().Be("broadcast");
        result.BotUsername.Should().BeNull();
        result.RawArgs.Should().Be("first_arg second_arg");
    }

    // verifies that commands followed by tabs extract the trigger and raw argument substring
    [Fact]
    public void ParseHeader_WhenCommandIsFollowedByTab_ShouldExtractCommandAndRawArgs()
    {
        // arrange
        var input = "/cmd\tparam1 param2";

        // act
        var result = CommandParser.ParseHeader(input);

        // assert
        result.Command.Should().Be("cmd");
        result.BotUsername.Should().BeNull();
        result.RawArgs.Should().Be("param1 param2");
    }

    // verifies that commands with bot targeting followed by a newline extract all header parts
    [Fact]
    public void ParseHeader_WhenCommandWithBotUsernameIsFollowedByNewline_ShouldExtractAllParts()
    {
        // arrange
        var input = "/notify@AlertBot\r\nurgent=true \"message with spaces\"";

        // act
        var result = CommandParser.ParseHeader(input);

        // assert
        result.Command.Should().Be("notify");
        result.BotUsername.Should().Be("AlertBot");
        result.RawArgs.TrimStart().Should().Be("urgent=true \"message with spaces\"");
    }
}