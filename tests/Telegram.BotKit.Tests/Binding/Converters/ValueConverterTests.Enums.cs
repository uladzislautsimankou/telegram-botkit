using Telegram.BotKit.Tests.Shared;

namespace Telegram.BotKit.Tests.Binding.Converters;

public partial class ValueConverterTests
{
    [Theory]
    [InlineData("First", TestEnum.First)]
    [InlineData("second", TestEnum.Second)]
    [InlineData("1", TestEnum.First)]
    public void Convert_ShouldParseEnum_ValidValues(string input, object expected)
    {
        var success = _converter.TryConvert(input, typeof(TestEnum), out var result);

        Assert.True(success);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_ShouldThrow_WhenEnumNumberDoesNotExist()
    {
        Assert.Throws<FormatException>(() =>
        {
            _converter.TryConvert("99", typeof(TestEnum), out _);
        });
    }

    [Fact]
    public void Convert_ShouldThrow_WhenNullableEnumIsInvalid()
    {
        Assert.Throws<FormatException>(() =>
        {
            _converter.TryConvert("invalid_text", typeof(TestEnum?), out _);
        });
    }
}
