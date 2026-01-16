namespace Telegram.BotKit.Tests.Binding.Converters;

public partial class ValueConverterTests
{
    [Theory]
    [InlineData("2025-12-31", 2025, 12, 31)]
    [InlineData("12/31/2025", 2025, 12, 31)]
    [InlineData("31.12.2025", 2025, 12, 31)]
    [InlineData("31-12-2025", 2025, 12, 31)]
    [InlineData("2025/12/31", 2025, 12, 31)]
    public void Convert_ShouldHandleDateTime(string input, int year, int month, int day)
    {
        var success = _converter.TryConvert(input, typeof(DateTime), out var result);

        Assert.True(success);
        Assert.IsType<DateTime>(result);
        var date = (DateTime)result!;

        Assert.Equal(year, date.Year);
        Assert.Equal(month, date.Month);
        Assert.Equal(day, date.Day);
    }

    [Fact]
    public void Convert_ShouldThrow_WhenDateIsInvalid()
    {
        Assert.Throws<FormatException>(() =>
        {
            _converter.TryConvert("2025-13-01", typeof(DateTime), out _);
        });
    }

    [Fact]
    public void Convert_ShouldThrow_WhenFormatUnknown()
    {
        Assert.Throws<FormatException>(() =>
        {
            _converter.TryConvert("2025.Jan.01", typeof(DateTime), out _);
        });
    }
}
