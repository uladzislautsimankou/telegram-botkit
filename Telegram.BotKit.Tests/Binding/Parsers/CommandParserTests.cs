using Telegram.BotKit.Binding.Parsers;

namespace Telegram.BotKit.Tests.Binding.Parsers;

public class CommandParserTests
{
    [Fact]
    public void Parse_ShouldReturnEmpty_WhenInputIsNull()
    {
        var result = CommandParser.Parse(null);
        Assert.Equal(string.Empty, result.Command);
        Assert.Null(result.BotUsername);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void Parse_ShouldReturnEmpty_WhenInputNotCommand()
    {
        var result = CommandParser.Parse("just text");
        Assert.Equal(string.Empty, result.Command);
        Assert.Empty(result.Parameters);
    }

    [Theory]
    [InlineData("/start", "start", null)]
    [InlineData("/start@MyBot", "start", "MyBot")]
    [InlineData("/duel@GameBot rps", "duel", "GameBot")]
    public void Parse_ShouldExtractCommand_And_BotUsername(string input, string expectedCmd, string? expectedBot)
    {
        var result = CommandParser.Parse(input);

        Assert.Equal(expectedCmd, result.Command);
        Assert.Equal(expectedBot, result.BotUsername);
    }
}
