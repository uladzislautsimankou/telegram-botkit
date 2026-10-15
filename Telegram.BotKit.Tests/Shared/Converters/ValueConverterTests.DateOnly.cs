using FluentAssertions;

namespace Telegram.BotKit.Tests.Shared.Converters;

public sealed partial class ValueConverterTests
{
    // verifies that valid date strings are parsed into DateOnly correctly
    [Theory]
    [InlineData("2025-12-31", 2025, 12, 31)]
    [InlineData("31.12.2025", 2025, 12, 31)]
    [InlineData("31-12-2025", 2025, 12, 31)]
    [InlineData("12/31/2025", 2025, 12, 31)]
    [InlineData("2025/12/31", 2025, 12, 31)]
    public void TryConvert_WhenDateOnlyIsValid_ShouldConvertSuccessfully(string input, int year, int month, int day)
    {
        // arrange
        var targetType = typeof(DateOnly);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(new DateOnly(year, month, day));
    }

    // verifies that invalid DateOnly strings throw format exception
    [Theory]
    [InlineData("2025-13-01")]
    [InlineData("invalid_date")]
    [InlineData("31.12.2025 14:30")] // DateOnly must reject time components
    public void Convert_WhenDateOnlyIsInvalid_ShouldThrowFormatException(string input)
    {
        // arrange
        var targetType = typeof(DateOnly);

        // act
        var act = () => _converter.Convert(input, targetType);

        // assert
        act.Should().Throw<FormatException>();
    }
}
