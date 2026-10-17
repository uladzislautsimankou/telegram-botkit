using System.Globalization;
using FluentAssertions;

namespace Telegram.BotKit.Tests.Shared.Converters;

public sealed partial class ValueConverterTests
{
    // verifies that malformed input never converts: TryConvert reports failure, Convert throws a descriptive format exception
    [Theory]
    [InlineData("not-a-number", typeof(int))]
    [InlineData("0x1F", typeof(int))] // hex notation is not supported
    [InlineData("not-a-boolean", typeof(bool))]
    [InlineData("not-a-guid", typeof(Guid))]
    [InlineData("1.2.3", typeof(double))]
    [InlineData("nope", typeof(DateTime))]
    [InlineData("nope", typeof(DateOnly))]
    [InlineData("25:99", typeof(TimeOnly))]
    [InlineData("Third", typeof(TestEnum))]
    [InlineData("Read, Nope", typeof(TestFlagsEnum))]
    [InlineData("1.2.x", typeof(Version))]
    public void MalformedInput_ShouldFailWithoutConverting(string input, Type targetType)
    {
        // act
        var success = _converter.TryConvert(input, targetType, out var result);
        var act = () => _converter.Convert(input, targetType);

        // assert
        success.Should().BeFalse();
        result.Should().BeNull();
        act.Should().Throw<FormatException>().WithMessage($"*{input}*");
    }

    // verifies that IParsable types and types known only to TypeDescriptor are converted
    [Theory]
    [InlineData("42", typeof(long), "42")]
    [InlineData("12,5", typeof(float), "12.5")]
    [InlineData("-0.25", typeof(decimal), "-0.25")]
    [InlineData("00:05:00", typeof(TimeSpan), "00:05:00")]
    [InlineData("1.2.3", typeof(Version), "1.2.3")]
    [InlineData("https://example.com/a", typeof(Uri), "https://example.com/a")]
    public void TryConvert_WhenTypeIsParsableOrKnownToTypeDescriptor_ShouldConvert(string input, Type targetType, string expectedText)
    {
        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().BeOfType(targetType);
        Convert.ToString(result, CultureInfo.InvariantCulture).Should().Be(expectedText);
    }

    // verifies that nullable targets are converted to the underlying value type
    [Theory]
    [InlineData("5", typeof(int?))]
    [InlineData("2025-01-02", typeof(DateOnly?))]
    [InlineData("First", typeof(TestEnum?))]
    public void TryConvert_WhenTargetIsNullableAndInputIsValid_ShouldConvertToUnderlyingValue(string input, Type targetType)
    {
        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().BeOfType(Nullable.GetUnderlyingType(targetType)!);
    }
}
