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

    [Fact]
    public void Parse_ShouldHandlePositionalArgs()
    {
        // обычные неименованные
        var input = "/cmd one two three";
        var result = CommandParser.Parse(input);

        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("one", result.Parameters[0]);
        Assert.Equal("two", result.Parameters[1]);
        Assert.Equal("three", result.Parameters[2]);
    }

    [Fact]
    public void Parse_ShouldHandleQuotedPositionalArgs()
    {
        // неименованный с кавычками
        var input = "/cmd \"John Doe\" 25";
        var result = CommandParser.Parse(input);

        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal("John Doe", result.Parameters[0]);
        Assert.Equal("25", result.Parameters[1]);
    }

    [Fact]
    public void Parse_ShouldHandleNamedArgs_Simple()
    {
        // обычные именованные
        var input = "/cmd name=Alex age=30";
        var result = CommandParser.Parse(input);

        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal("name=Alex", result.Parameters[0]);
        Assert.Equal("age=30", result.Parameters[1]);
    }

    [Fact]
    public void Parse_ShouldHandleNamedArgs_Quoted()
    {
        // именованные с кавычками
        var input = "/cmd name=\"Alex Smith\" city=\"New York\"";
        var result = CommandParser.Parse(input);

        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal("name=Alex Smith", result.Parameters[0]);
        Assert.Equal("city=New York", result.Parameters[1]);
    }

    [Fact]
    public void Parse_ShouldHandleMixedArgs()
    {
        // позиционный в кавычках, именованный в кавычках, обычный
        var input = "/cmd \"Pos Value\" key=\"Named Value\" simple";

        var result = CommandParser.Parse(input);

        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("Pos Value", result.Parameters[0]);
        Assert.Equal("key=Named Value", result.Parameters[1]);
        Assert.Equal("simple", result.Parameters[2]);
    }

    [Fact]
    public void Parse_ShouldIgnoreExtraSpaces()
    {
        // лишние пробелы между аргументами не должны создавать пустых параметров
        var input = "/cmd    one     \"two\"   key=\"val\"";
        var result = CommandParser.Parse(input);

        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("one", result.Parameters[0]);
        Assert.Equal("two", result.Parameters[1]);
        Assert.Equal("key=val", result.Parameters[2]);
    }

    [Fact]
    public void Parse_ShouldHandleEmptyQuotes()
    {
        // пустая строка в кавычках должна стать пустой строкой, а не исчезнуть
        var input = "/cmd \"\" name=\"\"";
        var result = CommandParser.Parse(input);

        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal("", result.Parameters[0]);
        Assert.Equal("name=", result.Parameters[1]);
    }
}
