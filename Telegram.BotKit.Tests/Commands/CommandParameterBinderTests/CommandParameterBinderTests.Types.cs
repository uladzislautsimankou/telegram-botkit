using FluentAssertions;

namespace Telegram.BotKit.Tests.Commands.CommandParameterBinderTests;

public sealed partial class CommandParameterBinderTests
{
    // verifies that complex parameters like guids and formatted datetimes are successfully parsed and mapped
    [Fact]
    public void Bind_WhenComplexTypesAreProvided_ShouldConvertAndMapProperties()
    {
        // arrange
        var guid = Guid.NewGuid();
        var ctx = CreateContext($"/cmd SomeName 1 First 31.12.2025 active=false uid={guid}");

        // act
        var result = _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        result.Date.Should().Be(new DateTime(2025, 12, 31));
        result.UserId.Should().Be(guid);
        result.IsActive.Should().BeFalse();
    }

    // verifies that integer-based string representations of enums are correctly mapped to enum properties
    [Fact]
    public void Bind_WhenEnumIsProvidedAsIntegerString_ShouldMapToEnumProperty()
    {
        // arrange
        var ctx = CreateContext("/cmd SomeName 1 1 2025-01-01 active=true");

        // act
        var result = _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        result.Status.Should().Be(TestEnum.First);
    }

    // verifies that positional arguments enclosed in quotes are parsed with whitespace preserved and quotes stripped
    [Fact]
    public void Bind_WhenQuotedStringIsProvidedAsPositional_ShouldMapAndStripQuotes()
    {
        // arrange
        var ctx = CreateContext("/cmd \"John Doe\" 25 First 2025-01-01 active=true");

        // act
        var result = _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        result.Name.Should().Be("John Doe");
        result.Age.Should().Be(25);
    }

    // verifies that named arguments enclosed in quotes are parsed with quotes stripped and value bound correctly
    [Fact]
    public void Bind_WhenQuotedStringIsProvidedAsNamed_ShouldMapAndStripQuotes()
    {
        // arrange
        var ctx = CreateContext("/cmd Name=\"John Doe\" 25 First 2025-01-01 active=true");

        // act
        var result = _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        result.Name.Should().Be("John Doe");
        result.Age.Should().Be(25);
    }
}
