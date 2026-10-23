using FluentAssertions;

namespace Telegram.BotKit.Tests.Commands.CommandParameterBinderTests;

public sealed partial class CommandParameterBinderTests
{
    // verifies that Position defines the binding order only, so gaps between positions do not shift arguments
    [Fact]
    public void Bind_WhenPositionsHaveGaps_ShouldBindArgumentsInOrder()
    {
        // act
        var result = _binder.Bind<GappedCommandParams>(CreateContext("/cmd alpha 7"));

        // assert
        result.First.Should().Be("alpha");
        result.Second.Should().Be(7);
    }

    // verifies that a parameter supplied by name does not consume a positional value
    [Fact]
    public void Bind_WhenEarlierPositionalParameterIsNamed_ShouldGiveValueToNextPositionalParameter()
    {
        // act
        var result = _binder.Bind<GappedCommandParams>(CreateContext("/cmd First=alpha 7"));

        // assert
        result.First.Should().Be("alpha");
        result.Second.Should().Be(7);
    }

    // verifies that only the last positional string takes the remaining values while earlier strings take exactly one
    [Fact]
    public void Bind_WhenLastPositionalParameterIsString_ShouldJoinRemainingValues()
    {
        // act
        var result = _binder.Bind<TwoStringsCommandParams>(CreateContext("/say hello big wide world"));

        // assert
        result.Verb.Should().Be("hello");
        result.Text.Should().Be("big wide world");
    }

    // verifies that a named value for the last positional string wins and the leftovers are ignored
    [Fact]
    public void Bind_WhenLastPositionalStringIsSuppliedByName_ShouldIgnoreLeftoverPositionalValues()
    {
        // act
        var result = _binder.Bind<SearchCommandParams>(CreateContext("/search Query=abc def"));

        // assert
        result.Query.Should().Be("abc");
    }

    // verifies that surplus positional values are ignored when the last positional parameter is not a string
    [Fact]
    public void Bind_WhenLastPositionalParameterIsNotString_ShouldIgnoreSurplusValues()
    {
        // act
        var result = _binder.Bind<ComplexCommandParams>(
            CreateContext("/cmd Bob 30 First 2025-01-01 extra tokens active=true"));

        // assert
        result.Name.Should().Be("Bob");
        result.Date.Should().Be(new DateTime(2025, 1, 1));
    }

    // verifies that duplicate positions are rejected instead of depending on reflection order
    [Fact]
    public void Bind_WhenModelDeclaresDuplicatePositions_ShouldThrowInvalidOperationException()
    {
        // act
        var act = () => _binder.Bind<DuplicatePositionParams>(CreateContext("/cmd a b"));

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Position 0*");
    }

    // verifies that duplicate parameter names are rejected, names being case-insensitive
    [Fact]
    public void Bind_WhenModelDeclaresDuplicateNames_ShouldThrowInvalidOperationException()
    {
        // act
        var act = () => _binder.Bind<DuplicateNameParams>(CreateContext("/cmd item=x"));

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*more than once*");
    }
}
