using FluentAssertions;
using Telegram.BotKit.Callbacks.Attributes;

namespace Telegram.BotKit.Tests.Callbacks.CallbackParameterBinderTests;

public sealed partial class CallbackParameterBinderTests
{
    private sealed class UnsupportedValue;

    private record UnsupportedTypeCallbackParams
    {
        public UnsupportedValue? Value { get; init; }
    }

    private sealed class WriteOnlyCallbackParams
    {
        private string? _secret;

        public int Id { get; set; }

        public string Secret
        {
            set => _secret = value;
        }

        public string? ReadSecret() => _secret;
    }

    // verifies that a property without a getter is not a parameter, because the data builder could never write it
    [Fact]
    public void Bind_WhenPropertyHasNoGetter_ShouldNotBindIt()
    {
        var result = _binder.Bind<WriteOnlyCallbackParams>(CreateContext("test?id=5&secret=value"));

        result.Id.Should().Be(5);
        result.ReadSecret().Should().BeNull();
    }

    // verifies that a valid model produces no startup problems
    [Fact]
    public void Validate_WhenModelIsValid_ShouldReturnNoProblems()
    {
        _binder.Validate(typeof(TestCallbackParams)).Should().BeEmpty();
        _binder.Validate(typeof(WriteOnlyCallbackParams)).Should().BeEmpty();
    }

    // verifies that duplicate parameter names are reported without binding any input
    [Fact]
    public void Validate_WhenModelDeclaresDuplicateNames_ShouldReportIt()
    {
        var problems = _binder.Validate(typeof(DuplicateNameCallbackParams));

        problems.Should().ContainSingle().Which.Should().Contain("DuplicateNameCallbackParams").And.Contain("more than once");
    }

    // verifies that a property type without a converter is reported instead of failing on every incoming callback
    [Fact]
    public void Validate_WhenPropertyTypeHasNoConverter_ShouldNameTheProperty()
    {
        var problems = _binder.Validate(typeof(UnsupportedTypeCallbackParams));

        problems.Should().ContainSingle()
            .Which.Should().Contain("UnsupportedTypeCallbackParams.Value").And.Contain("IValueConverter");
    }

    // verifies that a binder without any converter reports it as a problem of the model instead of crashing
    [Fact]
    public void Validate_WhenNoConvertersAreRegistered_ShouldReportProblem()
    {
        var binder = new Telegram.BotKit.Callbacks.Binding.CallbackParameterBinder([]);

        binder.Validate(typeof(TestCallbackParams)).Should().ContainSingle().Which.Should().Contain("No IValueConverter");
    }
}
