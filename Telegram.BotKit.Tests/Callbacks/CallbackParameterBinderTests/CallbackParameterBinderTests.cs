using FluentAssertions;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Callbacks.CallbackParameterBinderTests;

public sealed partial class CallbackParameterBinderTests
{
    // verifies that the binder maps query parameters by ignoring their casing
    [Fact]
    public void Bind_WhenStandardPropertiesAreProvidedIgnoreCase_ShouldMapToModel()
    {
        // arrange
        var ctx = CreateContext("test?id=42");

        // act
        var result = _binder.Bind<TestCallbackParams>(ctx);

        // assert
        result.Id.Should().Be(42);
    }

    // verifies that parameters with custom json property names are mapped correctly using their aliases
    [Fact]
    public void Bind_WhenAliasesAreProvided_ShouldMapToModel()
    {
        // arrange
        var ctx = CreateContext("test?s=ok&a=true");

        // act
        var result = _binder.Bind<TestCallbackParams>(ctx);

        // assert
        result.Status.Should().Be("ok");
        result.IsActive.Should().BeTrue();
    }

    // verifies that the binder converts various formats of enums including string names and integer values
    [Theory]
    [InlineData("test?testEnum=First", TestEnum.First)]
    [InlineData("test?testEnum=second", TestEnum.Second)]
    [InlineData("test?testEnum=1", TestEnum.First)]
    public void Bind_WhenEnumFormatsAreProvided_ShouldMapToExpectedMember(string data, TestEnum expected)
    {
        // arrange
        var ctx = CreateContext(data);

        // act
        var result = _binder.Bind<TestCallbackParams>(ctx);

        // assert
        result.TestEnum.Should().Be(expected);
    }

    // verifies that guid string values are successfully mapped to guid properties
    [Fact]
    public void Bind_WhenGuidIsProvided_ShouldMapToModel()
    {
        // arrange
        var guid = Guid.NewGuid();
        var ctx = CreateContext($"test?uid={guid}");

        // act
        var result = _binder.Bind<TestCallbackParams>(ctx);

        // assert
        result.Uid.Should().Be(guid);
    }

    // verifies that datetime string values in supported formats are parsed and assigned correctly
    [Fact]
    public void Bind_WhenDateTimeIsProvided_ShouldMapToModel()
    {
        // arrange
        var ctx = CreateContext("test?date=2025-12-31");

        // act
        var result = _binder.Bind<TestCallbackParams>(ctx);

        // assert
        result.Date.Should().Be(new DateTime(2025, 12, 31));
    }

    // verifies that parameters not present in the model are safely ignored during binding
    [Fact]
    public void Bind_WhenUnknownParamsAreProvided_ShouldIgnoreThem()
    {
        // arrange
        var ctx = CreateContext("test?id=1&unknown=val");

        // act
        var result = _binder.Bind<TestCallbackParams>(ctx);

        // assert
        result.Id.Should().Be(1);
    }

    // verifies that binding fails and throws an exception with correct metadata when a parameter type mismatch is encountered
    [Fact]
    public void Bind_WhenTypeMismatchOccurs_ShouldThrowInvalidParameterTypeException()
    {
        // arrange
        var ctx = CreateContext("test?id=not-a-number");

        // act
        var act = () => _binder.Bind<TestCallbackParams>(ctx);

        // assert
        act.Should().Throw<InvalidParameterTypeException>()
            .Which.ParameterName.Should().Be("id");
    }

    // verifies that the thrown exception contains the raw alias parameter name when its type matching fails
    [Fact]
    public void Bind_WhenAliasTypeMismatchOccurs_ShouldThrowInvalidParameterTypeExceptionWithAliasName()
    {
        // arrange
        var ctx = CreateContext("test?a=invalid-bool");

        // act
        var act = () => _binder.Bind<TestCallbackParams>(ctx);

        // assert
        act.Should().Throw<InvalidParameterTypeException>()
            .Which.ParameterName.Should().Be("a");
    }

    // verifies that empty query parameter values are mapped to empty strings for string properties
    [Fact]
    public void Bind_WhenEmptyStringValueIsProvided_ShouldMapToEmptyString()
    {
        // arrange
        var ctx = CreateContext("test?s=");

        // act
        var result = _binder.Bind<TestCallbackParams>(ctx);

        // assert
        result.Status.Should().Be(string.Empty);
    }
}
