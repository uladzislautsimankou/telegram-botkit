using FluentAssertions;

namespace Telegram.BotKit.Tests.Shared.Converters;

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

    // verifies that dates with textual month representations are parsed correctly
    [Theory]
    [InlineData("2025.Jan.01", 2025, 1, 1)]
    [InlineData("01-Jan-2025", 2025, 1, 1)]
    [InlineData("Jan 01, 2025", 2025, 1, 1)]
    public void TryConvert_WhenDateHasTextualMonth_ShouldConvertSuccessfully(
        string input, int year, int month, int day)
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

    // verifies that a format exception is thrown when the input is completely unparseable as a date
    [Theory]
    [InlineData("2025-13-01")]
    [InlineData("not-a-date")]
    [InlineData("2025-99-99")]
    [InlineData("2025.13.45")]
    [InlineData("32.01.2025")]
    public void Convert_WhenDateFormatIsInvalid_ShouldThrowFormatException(string invalidInput)
    {
        // arrange
        var targetType = typeof(DateTime);

        // act
        var act = () => _converter.Convert(invalidInput, targetType);

        // assert
        act.Should().Throw<FormatException>();
    }

    // verifies that datetime strings with various time formats and separators are parsed correctly
    [Theory]
    [InlineData("2025-12-31 14:30:45", 2025, 12, 31, 14, 30, 45)]
    [InlineData("31.12.2025 14:30:45", 2025, 12, 31, 14, 30, 45)]
    [InlineData("31.12.2025 14:30", 2025, 12, 31, 14, 30, 0)]
    [InlineData("2025-12-31T14:30:45", 2025, 12, 31, 14, 30, 45)]
    [InlineData("31-12-2025T14:30", 2025, 12, 31, 14, 30, 0)] // custom composite case
    public void TryConvert_WhenDateTimeWithTimeIsValid_ShouldConvertSuccessfully(
        string input, int year, int month, int day, int hour, int minute, int second)
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
        date.Hour.Should().Be(hour);
        date.Minute.Should().Be(minute);
        date.Second.Should().Be(second);
    }

    // verifies that date-only inputs convert to datetime with midnight time
    [Fact]
    public void TryConvert_WhenDateTimeHasNoTimeComponent_ShouldSetTimeToMidnight()
    {
        // arrange
        var input = "31.12.2025";
        var targetType = typeof(DateTime);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        var date = (DateTime)result!;
        date.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    // verifies that ambiguous date strings prioritize day-month-year ordering over invariant month-day-year
    [Theory]
    [InlineData("01.02.2025", 2025, 2, 1)]
    [InlineData("01-02-2025", 2025, 2, 1)]
    [InlineData("01.02.2025 14:30", 2025, 2, 1)]
    public void TryConvert_WhenDateStringIsAmbiguous_ShouldPrioritizeDayMonthYearOrder(
        string input, int expectedYear, int expectedMonth, int expectedDay)
    {
        // arrange
        var targetType = typeof(DateTime);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().BeOfType<DateTime>();

        var date = (DateTime)result!;
        date.Year.Should().Be(expectedYear);
        date.Month.Should().Be(expectedMonth);
        date.Day.Should().Be(expectedDay);
    }
}
