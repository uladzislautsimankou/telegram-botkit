using FluentAssertions;

namespace Telegram.BotKit.Tests.Shared.Converters;

public sealed partial class ValueConverterTests
{
    // verifies that standard primitives can be parsed and converted successfully
    [Theory]
    [InlineData("123", typeof(int), 123)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("False", typeof(bool), false)]
    public void TryConvert_WhenPrimitiveTypeIsValid_ShouldConvertSuccessfully(string input, Type type, object expected)
    {
        // arrange & act
        var success = _converter.TryConvert(input, type, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    // verifies that fractional double strings with dot or comma decimal marks are parsed correctly
    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData("12,5", 12.5)]
    public void TryConvert_WhenDoubleIsValid_ShouldConvertSuccessfully(string input, double expected)
    {
        // arrange
        var targetType = typeof(double);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    // verifies that direct strings bypass converter parsing and are returned as-is
    [Fact]
    public void TryConvert_WhenTargetTypeIsString_ShouldReturnInputDirectly()
    {
        // arrange
        var input = "hello";
        var targetType = typeof(string);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be("hello");
    }

    // verifies that an invalid integer string throws a format exception
    [Fact]
    public void TryConvert_WhenIntIsInvalid_ShouldThrowFormatException()
    {
        // arrange
        var invalidInt = "not-a-number";
        var targetType = typeof(int);

        // act
        var act = () => _converter.TryConvert(invalidInt, targetType, out _);

        // assert
        act.Should().Throw<FormatException>();
    }

    // verifies that unsupported target types without built-in string converters fail gracefully
    [Fact]
    public void TryConvert_WhenTypeHasNoDefaultConverter_ShouldReturnFalseAndNull()
    {
        // arrange
        var input = "something";
        var targetType = typeof(NoConverterType);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    // verifies that empty input returns success and null for nullable value types
    [Fact]
    public void TryConvert_WhenInputIsEmptyAndTypeIsNullableValueType_ShouldReturnTrueAndNull()
    {
        // arrange
        var input = "";
        var targetType = typeof(int?);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().BeNull();
    }

    // verifies that empty input returns success and null for reference types like strings
    [Fact]
    public void TryConvert_WhenInputIsEmptyAndTypeIsString_ShouldReturnTrueAndNull()
    {
        // arrange
        var input = "";
        var targetType = typeof(string);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().BeNull();
    }

    // verifies that empty input fails when attempting to bind to non-nullable value types
    [Fact]
    public void TryConvert_WhenInputIsEmptyAndTypeIsNotNullable_ShouldReturnFalse()
    {
        // arrange
        var input = "";
        var targetType = typeof(int);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeFalse();
    }

    // verifies that fractional float strings with dot or comma decimal marks are parsed correctly
    [Theory]
    [InlineData("3.14", 3.14f)]
    [InlineData("3,14", 3.14f)]
    public void TryConvert_WhenFloatIsValid_ShouldConvertSuccessfully(string input, float expected)
    {
        // arrange
        var targetType = typeof(float);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    // verifies that fractional decimal strings with dot or comma decimal marks are parsed correctly
    [Theory]
    [InlineData("100.50", "100.50")]
    [InlineData("100,50", "100.50")]
    public void TryConvert_WhenDecimalIsValid_ShouldConvertSuccessfully(string input, string expected)
    {
        // arrange
        var targetType = typeof(decimal);
        var expectedValue = decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(expectedValue);
    }

    // verifies that converting an invalid value for a type with a default descriptor throws format exception
    [Fact]
    public void TryConvert_WhenConversionFails_ShouldThrowFormatException()
    {
        // arrange
        var input = "not-a-boolean";
        var targetType = typeof(bool);

        // act
        var act = () => _converter.TryConvert(input, targetType, out _);

        // assert
        act.Should().Throw<FormatException>();
    }
}
