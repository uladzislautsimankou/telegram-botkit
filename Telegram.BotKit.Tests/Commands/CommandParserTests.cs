using FluentAssertions;
using Telegram.BotKit.Commands.Parsing;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandParserTests
{
    // verifies that passing a null command returns an empty trigger, null bot suffix, and no parameters
    [Fact]
    public void Parse_WhenInputIsNull_ShouldReturnEmptyCommandAndNullBotUsername()
    {
        // arrange
        string? input = null;

        // act
        var result = CommandParser.Parse(input);

        // assert
        result.Command.Should().BeEmpty();
        result.BotUsername.Should().BeNull();
        result.Parameters.Should().BeEmpty();
    }

    // verifies that texts without a leading slash are ignored and not treated as commands
    [Fact]
    public void Parse_WhenInputDoesNotStartWithSlash_ShouldReturnEmptyCommand()
    {
        // arrange
        var input = "just text";

        // act
        var result = CommandParser.Parse(input);

        // assert
        result.Command.Should().BeEmpty();
        result.Parameters.Should().BeEmpty();
    }

    // verifies that command triggers and explicit target bot usernames are extracted correctly
    [Theory]
    [InlineData("/start", "start", null)]
    [InlineData("/start@MyBot", "start", "MyBot")]
    [InlineData("/duel@GameBot rps", "duel", "GameBot")]
    public void Parse_WhenCommandWithBotUsernameSymmetric_ShouldExtractCommandAndBotUsername(string input, string expectedCmd, string? expectedBot)
    {
        // arrange & act
        var result = CommandParser.Parse(input);

        // assert
        result.Command.Should().Be(expectedCmd);
        result.BotUsername.Should().Be(expectedBot);
    }

    // verifies that passing an empty command string returns an empty command key and empty parameters
    [Fact]
    public void Parse_WhenInputIsEmpty_ShouldReturnEmptyCommandAndNullBotUsername()
    {
        // arrange
        var input = "";

        // act
        var result = CommandParser.Parse(input);

        // assert
        result.Command.Should().BeEmpty();
        result.BotUsername.Should().BeNull();
        result.Parameters.Should().BeEmpty();
    }

    // verifies that passing only a slash with no text returns an empty command key
    [Fact]
    public void Parse_WhenInputIsOnlySlash_ShouldReturnEmptyCommand()
    {
        // arrange
        var input = "/";

        // act
        var result = CommandParser.Parse(input);

        // assert
        result.Command.Should().BeEmpty();
        result.Parameters.Should().BeEmpty();
    }

    // verifies that commands followed by newlines extract the trigger and parse subsequent arguments
    [Fact]
    public void Parse_WhenCommandIsFollowedByNewline_ShouldExtractCommandAndParameters()
    {
        // arrange
        var input = "/broadcast\nfirst_arg second_arg";

        // act
        var result = CommandParser.Parse(input);

        // assert
        result.Command.Should().Be("broadcast");
        result.BotUsername.Should().BeNull();
        result.Parameters.Should().HaveCount(2);
        result.Parameters[0].Should().Be("first_arg");
        result.Parameters[1].Should().Be("second_arg");
    }

    // verifies that commands followed by tabs extract the trigger and parse subsequent arguments
    [Fact]
    public void Parse_WhenCommandIsFollowedByTab_ShouldExtractCommandAndParameters()
    {
        // arrange
        var input = "/cmd\tparam1 param2";

        // act
        var result = CommandParser.Parse(input);

        // assert
        result.Command.Should().Be("cmd");
        result.BotUsername.Should().BeNull();
        result.Parameters.Should().HaveCount(2);
        result.Parameters[0].Should().Be("param1");
        result.Parameters[1].Should().Be("param2");
    }

    // verifies that commands with bot targeting followed by a newline extract both trigger and bot username correctly
    [Fact]
    public void Parse_WhenCommandWithBotUsernameIsFollowedByNewline_ShouldExtractAllParts()
    {
        // arrange
        var input = "/notify@AlertBot\r\nurgent=true \"message with spaces\"";

        // act
        var result = CommandParser.Parse(input);

        // assert
        result.Command.Should().Be("notify");
        result.BotUsername.Should().Be("AlertBot");
        result.Parameters.Should().HaveCount(2);
        result.Parameters[0].Should().Be("urgent=true");
        result.Parameters[1].Should().Be("message with spaces");
    }
}