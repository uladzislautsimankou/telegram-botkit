using FluentAssertions;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Commands.CommandParameterBinderTests;

public sealed partial class CommandParameterBinderTests
{
    // verifies that missing required command parameters result in a missing parameter exception
    [Fact]
    public void Bind_WhenRequiredParameterIsMissing_ShouldThrowMissingParameterException()
    {
        // arrange
        var ctx = CreateContext("/cmd SomeName 25 First 2025-01-01");

        // act
        var act = () => _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        act.Should().Throw<MissingParameterException>()
            .Which.ParameterName.Should().Be("active");
    }

    // verifies that supplying an invalid value for an enum property throws an invalid parameter type exception
    [Fact]
    public void Bind_WhenEnumMismatchOccurs_ShouldThrowInvalidParameterTypeException()
    {
        // arrange
        var ctx = CreateContext("/cmd SomeName 25 99 2025-01-01 active=true");

        // act
        var act = () => _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        act.Should().Throw<InvalidParameterTypeException>()
            .Which.ParameterName.Should().Be("Status");
    }

    // verifies that supplying an invalid non-numeric value for an integer property throws an invalid parameter type exception
    [Fact]
    public void Bind_WhenIntegerMismatchOccurs_ShouldThrowInvalidParameterTypeException()
    {
        // arrange
        var ctx = CreateContext("/cmd SomeName old First 2025-01-01 active=true");

        // act
        var act = () => _binder.Bind<ComplexCommandParams>(ctx);

        // assert
        act.Should().Throw<InvalidParameterTypeException>()
            .Which.ParameterName.Should().Be("Age");
    }
}
