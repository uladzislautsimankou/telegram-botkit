using Telegram.BotKit.Binding.Parsers;

namespace Telegram.BotKit.Tests.Binding.Parsers;

public class CallbackParserTests
{
    [Fact]
    public void Parse_ShouldReturnEmpty_WhenInputIsNull()
    {
        var result = CallbackParser.Parse(null);
        Assert.Equal(string.Empty, result.Key);
        Assert.Empty(result.Parameters);
    }

    [Theory]
    [InlineData("menu", "menu", 0)]                     // Только ключ
    [InlineData("game:start?id=123", "game:start", 1)]  // Ключ + 1 параметр
    [InlineData("nav?to=settings&b=main", "nav", 2)]    // Ключ + 2 параметра
    public void Parse_ShouldExtractKeyAndParams(string input, string expectedKey, int expectedCount)
    {
        var result = CallbackParser.Parse(input);

        Assert.Equal(expectedKey, result.Key);
        Assert.Equal(expectedCount, result.Parameters.Count);
    }

    [Fact]
    public void Parse_ShouldHandleValues()
    {
        var input = "vote?id=5&opt=yes";
        var result = CallbackParser.Parse(input);

        Assert.Equal("5", result.Parameters["id"]);
        Assert.Equal("yes", result.Parameters["opt"]);
    }

    [Fact]
    public void Parse_ShouldHandleKeysCaseInsensitive()
    {
        var input = "test?ID=123";
        var result = CallbackParser.Parse(input);

        Assert.True(result.Parameters.ContainsKey("id"));
        Assert.Equal("123", result.Parameters["id"]);
    }

    [Fact]
    public void Parse_ShouldHandleEmptyValues()
    {
        // иногда бывает такое: key?param=
        var input = "filter?active=&id=1";
        var result = CallbackParser.Parse(input);

        Assert.True(result.Parameters.ContainsKey("active"));
        Assert.Equal(string.Empty, result.Parameters["active"]);
        Assert.Equal("1", result.Parameters["id"]);
    }

    [Fact]
    public void Parse_ShouldHandleNoParamsSeparator()
    {
        // eсли нет ?, это просто ключ
        var input = "simple_action";
        var result = CallbackParser.Parse(input);

        Assert.Equal("simple_action", result.Key);
        Assert.Empty(result.Parameters);
    }
}