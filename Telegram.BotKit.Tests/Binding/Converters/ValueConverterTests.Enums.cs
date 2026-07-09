using FluentAssertions;
using Telegram.BotKit.Tests.Shared;

namespace Telegram.BotKit.Tests.Binding.Converters;

public sealed partial class ValueConverterTests
{
    // verifies that valid enum string names or numerical values can be converted to the enum type
    [Theory]
    [InlineData("First", TestEnum.First)]
    [InlineData("second", TestEnum.Second)]
    [InlineData("1", TestEnum.First)]
    public void TryConvert_WhenEnumMemberIsValid_ShouldConvertSuccessfully(string input, object expected)
    {
        // arrange
        var targetType = typeof(TestEnum);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    // verifies that an undefined enum numerical value throws a format exception
    [Fact]
    public void TryConvert_WhenEnumIntIsUndefined_ShouldThrowFormatException()
    {
        // arrange
        var undefinedValue = "99";
        var targetType = typeof(TestEnum);

        // act
        var act = () => _converter.TryConvert(undefinedValue, targetType, out _);

        // assert
        act.Should().Throw<FormatException>();
    }

    // verifies that parsing an invalid string value for a nullable enum throws a format exception
    [Fact]
    public void TryConvert_WhenNullableEnumStringIsInvalid_ShouldThrowFormatException()
    {
        // arrange
        var invalidValue = "invalid_text";
        var targetType = typeof(TestEnum?);

        // act
        var act = () => _converter.TryConvert(invalidValue, targetType, out _);

        // assert
        act.Should().Throw<FormatException>();
    }
}