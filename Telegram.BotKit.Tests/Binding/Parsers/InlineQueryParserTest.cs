using Telegram.BotKit.Binding.Parsers;

namespace Telegram.BotKit.Tests.Binding.Parsers;

public class InlineQueryParserTest
{
    [Fact]
    public void Parse_ShouldReturnEmpty_WhenInputIsNull()
    {
        var result = InlineQueryParser.Parse(null);
        Assert.Equal(string.Empty, result.Key);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void Parse_ShouldExtractKey()
    {
        var input = "find";
        var result = InlineQueryParser.Parse(input);

        Assert.Equal("find", result.Key);
    }

    [Fact]
    public void Parse_ShouldExtractKey_And_Parameters()
    {
        var input = "find one two three";
        var result = InlineQueryParser.Parse(input);

        Assert.Equal("find", result.Key);
        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("one", result.Parameters[0]);
        Assert.Equal("two", result.Parameters[1]);
        Assert.Equal("three", result.Parameters[2]);
    }
}
