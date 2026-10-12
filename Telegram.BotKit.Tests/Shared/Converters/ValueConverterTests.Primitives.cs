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
        // act
        var success = _converter.TryConvert(input, type, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    // verifies that valid guid strings are parsed successfully
    [Theory]
    [InlineData("9f826315-9c59-4f76-8800-4b89823f6608")]
    [InlineData("9F826315-9C59-4F76-8800-4B89823F6608")]
    [InlineData("9f8263159c594f7688004b89823f6608")]
    public void TryConvert_WhenGuidIsValid_ShouldConvertSuccessfully(string input)
    {
        // arrange
        var targetType = typeof(Guid);
        var expectedGuid = Guid.Parse(input);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be(expectedGuid);
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
    public void Convert_WhenIntIsInvalid_ShouldThrowFormatException()
    {
        // arrange
        var invalidInt = "not-a-number";
        var targetType = typeof(int);

        // act
        var act = () => _converter.Convert(invalidInt, targetType);

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

    // verifies that whitespace-only input returns success and null for nullable value types
    [Fact]
    public void TryConvert_WhenInputIsWhitespaceAndTypeIsNullableValueType_ShouldReturnTrueAndNull()
    {
        // arrange
        var input = "   ";
        var targetType = typeof(int?);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().BeNull();
    }

    // verifies that empty input remains an empty string when target type is string
    [Fact]
    public void TryConvert_WhenInputIsEmptyAndTypeIsString_ShouldReturnEmptyString()
    {
        // arrange
        var input = "";
        var targetType = typeof(string);

        // act
        var success = _converter.TryConvert(input, targetType, out var result);

        // assert
        success.Should().BeTrue();
        result.Should().Be("");
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

    // verifies that whitespace-only input fails when attempting to bind to non-nullable value types
    [Fact]
    public void TryConvert_WhenInputIsWhitespaceAndTypeIsNotNullable_ShouldReturnFalse()
    {
        // arrange
        var input = "   ";
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
    public void Convert_WhenConversionFails_ShouldThrowFormatException()
    {
        // arrange
        var input = "not-a-boolean";
        var targetType = typeof(bool);

        // act
        var act = () => _converter.Convert(input, targetType);

        // assert
        act.Should().Throw<FormatException>();
    }
}