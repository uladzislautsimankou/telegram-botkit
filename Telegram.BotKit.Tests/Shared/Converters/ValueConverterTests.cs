using FluentAssertions;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Tests.Shared.Converters;

public sealed partial class ValueConverterTests
{
    private sealed class NoConverterType;

    private readonly IValueConverter _converter = new DefaultValueConverter();

    // verifies that supported types return true in canconvert
    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(int?))]
    [InlineData(typeof(string))]
    [InlineData(typeof(double))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(DateOnly?))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(TimeOnly?))]
    [InlineData(typeof(TestEnum))]
    [InlineData(typeof(TestFlagsEnum))]
    [InlineData(typeof(long))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(Version))]
    [InlineData(typeof(Uri))]
    public void CanConvert_WhenTypeIsSupported_ShouldReturnTrue(Type targetType)
    {
        // act
        var result = _converter.CanConvert(targetType);

        // assert
        result.Should().BeTrue();
    }

    // verifies that unsupported custom types return false in canconvert
    [Fact]
    public void CanConvert_WhenTypeIsNotSupported_ShouldReturnFalse()
    {
        // act
        var result = _converter.CanConvert(typeof(NoConverterType));

        // assert
        result.Should().BeFalse();
    }

    // verifies that convert throws not supported exception when no conversion strategy matches
    [Fact]
    public void Convert_WhenTargetTypeIsNotSupported_ShouldThrowNotSupportedException()
    {
        // arrange
        var targetType = typeof(NoConverterType);

        // act
        var act = () => _converter.Convert("test", targetType);

        // assert
        act.Should().Throw<NotSupportedException>();
    }
}
