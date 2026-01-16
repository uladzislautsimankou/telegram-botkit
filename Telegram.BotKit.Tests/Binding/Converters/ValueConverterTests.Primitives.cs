namespace Telegram.BotKit.Tests.Binding.Converters;

public partial class ValueConverterTests
{
    [Theory]
    [InlineData("123", typeof(int), 123)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("False", typeof(bool), false)]
    public void Convert_ShouldWorkForPrimitives(string input, Type type, object expected)
    {
        var success = _converter.TryConvert(input, type, out var result);

        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData("12,5", 12.5)]
    public void Convert_ShouldHandleDouble(string input, double expected)
    {
        var success = _converter.TryConvert(input, typeof(double), out var result);

        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_ShouldReturnInputString_WhenTargetIsString()
    {
        var success = _converter.TryConvert("hello", typeof(string), out var result);

        Assert.True(success);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Convert_ShouldThrow_WhenInvalidInt()
    {
        Assert.Throws<FormatException>(() =>
        {
            _converter.TryConvert("not-a-number", typeof(int), out _);
        });
    }

    [Fact]
    public void Convert_ShouldReturnFalse_WhenNoTypeConverter()
    {
        var success = _converter.TryConvert("something", typeof(NoConverterType), out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void Convert_ShouldReturnTrue_AndNull_ForEmptyString_IfNullable()
    {
        // int? может быть null
        var success = _converter.TryConvert("", typeof(int?), out var result);

        Assert.True(success);
        Assert.Null(result);
    }

    [Fact]
    public void Convert_ShouldReturnTrue_AndNull_ForEmptyString_IfString()
    {
        // string может быть null
        var success = _converter.TryConvert("", typeof(string), out var result);

        Assert.True(success);
        Assert.Null(result);
    }

    [Fact]
    public void Convert_ShouldReturnFalse_ForEmptyString_IfNotNullable()
    {
        // int не может быть null
        var success = _converter.TryConvert("", typeof(int), out var result);

        Assert.False(success);
    }
}
