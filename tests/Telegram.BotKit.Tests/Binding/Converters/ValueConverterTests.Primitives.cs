using Telegram.BotKit.Binding.Converters;

namespace Telegram.BotKit.Tests.Binding.Converters;

public partial class ValueConverterTests
{
    [Theory]
    [InlineData("123", typeof(int), 123)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("False", typeof(bool), false)]
    public void Convert_ShouldWorkForPrimitives(string input, Type type, object expected)
    {
        var result = ValueConverter.Convert(input, type);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData("12,5", 12.5)]
    public void Convert_ShouldHandleDouble(string input, double expected)
    {
        var result = ValueConverter.Convert(input, typeof(double));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_ShouldReturnInputString_WhenTargetIsString()
    {
        var result = ValueConverter.Convert("hello", typeof(string));
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Convert_ShouldThrow_WhenInvalidInt()
    {
        Assert.Throws<FormatException>(() =>
        {
            ValueConverter.Convert("not-a-number", typeof(int));
        });
    }

    [Fact]
    public void Convert_ShouldThrow_WhenNoTypeConverter()
    {
        Assert.Throws<NotSupportedException>(() =>
        {
            ValueConverter.Convert("something", typeof(NoConverterType));
        });
    }
}
