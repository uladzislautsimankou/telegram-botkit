using FluentAssertions;

namespace Telegram.BotKit.Tests.Shared.Converters;

public sealed partial class ValueConverterTests
{
    private enum TestEnum
    {
        First = 1,
        Second = 2
    }

    [Flags]
    private enum TestFlagsEnum
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }

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
    public void Convert_WhenEnumIntIsUndefined_ShouldThrowFormatException()
    {
        // arrange
        var undefinedValue = "99";
        var targetType = typeof(TestEnum);

        // act
        var act = () => _converter.Convert(undefinedValue, targetType);

        // assert
        act.Should().Throw<FormatException>();
    }

    // verifies that parsing an invalid string value for a nullable enum throws a format exception
    [Fact]
    public void Convert_WhenNullableEnumStringIsInvalid_ShouldThrowFormatException()
    {
        // arrange
        var invalidValue = "invalid_text";
        var targetType = typeof(TestEnum?);

        // act
        var act = () => _converter.Convert(invalidValue, targetType);

        // assert
        act.Should().Throw<FormatException>();
    }

    // verifies that valid flag enum combinations (by name, comma-separated, or number) are parsed correctly
    [Theory]
    [InlineData("None", TestFlagsEnum.None)]
    [InlineData("Read", TestFlagsEnum.Read)]
    [InlineData("Read, Write", TestFlagsEnum.Read | TestFlagsEnum.Write)]
    [InlineData("read,execute", TestFlagsEnum.Read | TestFlagsEnum.Execute)]
    [InlineData("3", TestFlagsEnum.Read | TestFlagsEnum.Write)]
    [InlineData("7", TestFlagsEnum.Read | TestFlagsEnum.Write | TestFlagsEnum.Execute)]
    public void TryConvert_WhenFlagsEnumIsValid_ShouldConvertSuccessfully(string input, object expected)
    {
        // arrange
        var targetType = typeof(TestFlagsEnum);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    // verifies that a flag combination containing undefined bits throws a format exception
    [Theory]
    [InlineData("8")]
    [InlineData("15")]
    [InlineData("Read, UnknownFlag")]
    public void Convert_WhenFlagsEnumHasUndefinedBits_ShouldThrowFormatException(string input)
    {
        // arrange
        var targetType = typeof(TestFlagsEnum);

        // act
        var act = () => _converter.Convert(input, targetType);

        // assert
        act.Should().Throw<FormatException>();
    }
}
