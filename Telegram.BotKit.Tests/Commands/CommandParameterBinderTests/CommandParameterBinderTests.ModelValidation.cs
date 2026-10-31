using FluentAssertions;
using Telegram.BotKit.Commands.Attributes;

namespace Telegram.BotKit.Tests.Commands.CommandParameterBinderTests;

public sealed partial class CommandParameterBinderTests
{
    private sealed class UnsupportedValue;

    private record UnsupportedTypeCommandParams
    {
        [CommandParam(Position = 0)]
        public UnsupportedValue? Value { get; init; }
    }

    // verifies that a valid model produces no startup problems
    [Fact]
    public void Validate_WhenModelIsValid_ShouldReturnNoProblems()
    {
        _binder.Validate(typeof(ComplexCommandParams)).Should().BeEmpty();
        _binder.Validate(typeof(Telegram.BotKit.Commands.Models.EmptyCommandParams)).Should().BeEmpty();
    }

    // verifies that duplicate positions are reported without binding any input
    [Fact]
    public void Validate_WhenModelDeclaresDuplicatePositions_ShouldReportIt()
    {
        var problems = _binder.Validate(typeof(DuplicatePositionParams));

        problems.Should().ContainSingle().Which.Should().Contain("DuplicatePositionParams").And.Contain("Position");
    }

    // verifies that duplicate parameter names are reported without binding any input
    [Fact]
    public void Validate_WhenModelDeclaresDuplicateNames_ShouldReportIt()
    {
        var problems = _binder.Validate(typeof(DuplicateNameParams));

        problems.Should().ContainSingle().Which.Should().Contain("more than once");
    }

    // verifies that a property type without a converter is reported instead of failing on every incoming command
    [Fact]
    public void Validate_WhenPropertyTypeHasNoConverter_ShouldNameTheProperty()
    {
        var problems = _binder.Validate(typeof(UnsupportedTypeCommandParams));

        problems.Should().ContainSingle()
            .Which.Should().Contain("UnsupportedTypeCommandParams.Value").And.Contain("IValueConverter");
    }

    // verifies that validation warms the plan cache, so the first real command binds with the validated plan
    [Fact]
    public void Validate_ThenBind_ShouldStillBindCorrectly()
    {
        _binder.Validate(typeof(SearchCommandParams)).Should().BeEmpty();

        var result = _binder.Bind<SearchCommandParams>(CreateContext("/search hello world"));

        result.Query.Should().Be("hello world");
    }
}
