using Telegram.BotKit.Binding.Converters;
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
        var result = ValueConverter.Convert(input, typeof(TestEnum));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_ShouldThrow_WhenEnumNumberDoesNotExist()
    {
        Assert.Throws<FormatException>(() =>
        {
            ValueConverter.Convert("99", typeof(TestEnum));
        });
    }

    [Fact]
    public void Convert_ShouldThrow_WhenNullableEnumIsInvalid()
    {
        Assert.Throws<FormatException>(() =>
        {
            ValueConverter.Convert("invalid_text", typeof(TestEnum?));
        });
    }
}
