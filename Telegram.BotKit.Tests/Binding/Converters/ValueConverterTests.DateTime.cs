using FluentAssertions;

namespace Telegram.BotKit.Tests.Binding.Converters;

public sealed partial class ValueConverterTests
{
    // verifies that valid datetime strings in various formats are parsed correctly
    [Theory]
    [InlineData("2025-12-31", 2025, 12, 31)]
    [InlineData("12/31/2025", 2025, 12, 31)]
    [InlineData("31.12.2025", 2025, 12, 31)]
    [InlineData("31-12-2025", 2025, 12, 31)]
    [InlineData("2025/12/31", 2025, 12, 31)]
    public void TryConvert_WhenDateTimeIsValid_ShouldConvertSuccessfully(string input, int year, int month, int day)
    {
        // arrange
        var targetType = typeof(DateTime);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().BeOfType<DateTime>();

        var date = (DateTime)result!;
        date.Year.Should().Be(year);
        date.Month.Should().Be(month);
        date.Day.Should().Be(day);
    }

    // verifies that a format exception is thrown when the parsed datetime contains out-of-range values
    [Fact]
    public void TryConvert_WhenDateValueIsInvalid_ShouldThrowFormatException()
    {
        // arrange
        var invalidDate = "2025-13-01";
        var targetType = typeof(DateTime);

        // act
        var act = () => _converter.TryConvert(invalidDate, targetType, out _);

        // assert
        act.Should().Throw<FormatException>();
    }

    // verifies that a format exception is thrown when the input datetime format is unsupported
    [Fact]
    public void TryConvert_WhenDateFormatIsUnsupported_ShouldThrowFormatException()
    {
        // arrange
        var invalidFormat = "2025.Jan.01";
        var targetType = typeof(DateTime);

        // act
        var act = () => _converter.TryConvert(invalidFormat, targetType, out _);

        // assert
        act.Should().Throw<FormatException>();
    }
}