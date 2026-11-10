using FluentAssertions;
using Telegram.BotKit.WidgetQueries.Binding;

namespace Telegram.BotKit.Tests.WidgetQueries.WidgetParameterBinderTests;

public sealed partial class WidgetParameterBinderTests
{
    private sealed class UnsupportedValue;

    private record UnsupportedTypeWidgetParams
    {
        public UnsupportedValue? Value { get; init; }
    }

    private sealed class WriteOnlyWidgetParams
    {
        private string? _secret;

        public int Id { get; set; }

        public string Secret
        {
            set => _secret = value;
        }

        public string? ReadSecret() => _secret;
    }

    // verifies that a property without a getter is not a parameter, because a data builder could never write it
    [Fact]
    public void Bind_WhenPropertyHasNoGetter_ShouldNotBindIt()
    {
        var result = _binder.Bind<WriteOnlyWidgetParams>(CreateContext("test?id=5&secret=value"));

        result.Id.Should().Be(5);
        result.ReadSecret().Should().BeNull();
    }

    // verifies that a valid model produces no startup problems
    [Fact]
    public void Validate_WhenModelIsValid_ShouldReturnNoProblems()
    {
        _binder.Validate(typeof(TestWidgetParams)).Should().BeEmpty();
        _binder.Validate(typeof(WriteOnlyWidgetParams)).Should().BeEmpty();
    }

    // verifies that duplicate parameter names are reported without binding any input
    [Fact]
    public void Validate_WhenModelDeclaresDuplicateNames_ShouldReportIt()
    {
        var problems = _binder.Validate(typeof(DuplicateNameWidgetParams));

        problems.Should().ContainSingle().Which.Should().Contain("DuplicateNameWidgetParams").And.Contain("more than once");
    }

    // verifies that a property type without a converter is reported instead of failing on every incoming query
    [Fact]
    public void Validate_WhenPropertyTypeHasNoConverter_ShouldNameTheProperty()
    {
        var problems = _binder.Validate(typeof(UnsupportedTypeWidgetParams));

        problems.Should().ContainSingle()
            .Which.Should().Contain("UnsupportedTypeWidgetParams.Value").And.Contain("IValueConverter");
    }

    // verifies that a binder without any converter reports it as a problem of the model instead of crashing
    [Fact]
    public void Validate_WhenNoConvertersAreRegistered_ShouldReportProblem()
    {
        var binder = new WidgetParameterBinder([]);

        binder.Validate(typeof(TestWidgetParams)).Should().ContainSingle().Which.Should().Contain("No IValueConverter");
    }
}
