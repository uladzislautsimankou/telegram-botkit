using Telegram.BotKit.Helpers;

namespace Telegram.BotKit.Tests.Helpers;

public class ArgumentParserTests
{
    [Fact]
    public void Parse_ShouldHandlePositionalArgs()
    {
        // обычные неименованные
        var input = "one two three";
        var result = ArgumentParser.ParseArgs(input);

        Assert.Equal(3, result.Count);
        Assert.Equal("one", result[0]);
        Assert.Equal("two", result[1]);
        Assert.Equal("three", result[2]);
    }

    [Fact]
    public void Parse_ShouldHandleQuotedPositionalArgs()
    {
        // неименованный с кавычками
        var input = "\"John Doe\" 25";
        var result = ArgumentParser.ParseArgs(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("John Doe", result[0]);
        Assert.Equal("25", result[1]);
    }

    [Fact]
    public void Parse_ShouldHandleNamedArgs_Simple()
    {
        // обычные именованные
        var input = "name=Alex age=30";
        var result = ArgumentParser.ParseArgs(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("name=Alex", result[0]);
        Assert.Equal("age=30", result[1]);
    }

    [Fact]
    public void Parse_ShouldHandleNamedArgs_Quoted()
    {
        // именованные с кавычками
        var input = "name=\"Alex Smith\" city=\"New York\"";
        var result = ArgumentParser.ParseArgs(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("name=Alex Smith", result[0]);
        Assert.Equal("city=New York", result[1]);
    }

    [Fact]
    public void Parse_ShouldHandleMixedArgs()
    {
        // позиционный в кавычках, именованный в кавычках, обычный
        var input = "\"Pos Value\" key=\"Named Value\" simple";

        var result = ArgumentParser.ParseArgs(input);

        Assert.Equal(3, result.Count);
        Assert.Equal("Pos Value", result[0]);
        Assert.Equal("key=Named Value", result[1]);
        Assert.Equal("simple", result[2]);
    }

    [Fact]
    public void Parse_ShouldIgnoreExtraSpaces()
    {
        // лишние пробелы между аргументами не должны создавать пустых параметров
        var input = "    one     \"two\"   key=\"val\"";
        var result = ArgumentParser.ParseArgs(input);

        Assert.Equal(3, result.Count);
        Assert.Equal("one", result[0]);
        Assert.Equal("two", result[1]);
        Assert.Equal("key=val", result[2]);
    }

    [Fact]
    public void Parse_ShouldHandleEmptyQuotes()
    {
        // пустая строка в кавычках должна стать пустой строкой, а не исчезнуть
        var input = "\"\" name=\"\"";
        var result = ArgumentParser.ParseArgs(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("", result[0]);
        Assert.Equal("name=", result[1]);
    }
}
