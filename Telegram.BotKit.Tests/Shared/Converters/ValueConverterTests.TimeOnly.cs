using FluentAssertions;

namespace Telegram.BotKit.Tests.Shared.Converters;

public sealed partial class ValueConverterTests
{
    // verifies that valid time strings in various notations are parsed into TimeOnly correctly
    [Theory]
    [InlineData("14:30:45", 14, 30, 45)]
    [InlineData("14:30", 14, 30, 0)]
    [InlineData("9:15", 9, 15, 0)]
    [InlineData("09:15:05", 9, 15, 5)]
    public void TryConvert_WhenTimeOnlyIsValid_ShouldConvertSuccessfully(string input, int hour, int minute, int second)
    {
        // arrange
        var targetType = typeof(TimeOnly);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(new TimeOnly(hour, minute, second));
    }

    // verifies that invalid TimeOnly strings throw format exception
    [Theory]
    [InlineData("25:00")]
    [InlineData("14:65")]
    [InlineData("invalid_time")]
    [InlineData("2025-12-31 14:30")] // TimeOnly must reject date components
    public void Convert_WhenTimeOnlyIsInvalid_ShouldThrowFormatException(string input)
    {
        // arrange
        var targetType = typeof(TimeOnly);

        // act
        var act = () => _converter.Convert(input, targetType);

        // assert
        act.Should().Throw<FormatException>();
    }
}