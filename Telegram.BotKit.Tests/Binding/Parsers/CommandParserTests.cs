using FluentAssertions;
using Telegram.BotKit.Binding.Parsers;

namespace Telegram.BotKit.Tests.Binding.Parsers;

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
}