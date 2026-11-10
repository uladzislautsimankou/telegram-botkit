using FluentAssertions;
using Telegram.BotKit.Shared.Exceptions;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.Tests.WidgetQueries.WidgetParameterBinderTests;

public sealed partial class WidgetParameterBinderTests
{
    // verifies that the binder maps query parameters by ignoring their casing
    [Fact]
    public void Bind_WhenStandardPropertiesAreProvidedIgnoreCase_ShouldMapToModel()
    {
        var result = _binder.Bind<TestWidgetParams>(CreateContext("test?ID=42"));

        result.Id.Should().Be(42);
    }

    // verifies that parameters with [WidgetParam] names are mapped correctly using their aliases
    [Fact]
    public void Bind_WhenAliasesAreProvided_ShouldMapToModel()
    {
        var result = _binder.Bind<TestWidgetParams>(CreateContext("test?s=ok&a=true"));

        result.Status.Should().Be("ok");
        result.IsActive.Should().BeTrue();
    }

    // verifies that the binder converts various formats of enums including string names and integer values
    [Theory]
    [InlineData("test?mode=First", WidgetTestEnum.First)]
    [InlineData("test?mode=second", WidgetTestEnum.Second)]
    [InlineData("test?mode=1", WidgetTestEnum.First)]
    public void Bind_WhenEnumFormatsAreProvided_ShouldMapToExpectedMember(string data, WidgetTestEnum expected)
    {
        var result = _binder.Bind<TestWidgetParams>(CreateContext(data));

        result.Mode.Should().Be(expected);
    }

    // verifies that guid string values are successfully mapped to guid properties
    [Fact]
    public void Bind_WhenGuidIsProvided_ShouldMapToModel()
    {
        var guid = Guid.NewGuid();

        var result = _binder.Bind<TestWidgetParams>(CreateContext($"test?uid={guid}"));

        result.Uid.Should().Be(guid);
    }

    // verifies that datetime string values in supported formats are parsed and assigned correctly
    [Fact]
    public void Bind_WhenDateTimeIsProvided_ShouldMapToModel()
    {
        var result = _binder.Bind<TestWidgetParams>(CreateContext("test?date=2025-12-31"));

        result.Date.Should().Be(new DateTime(2025, 12, 31));
    }

    // verifies that parameters not present in the model are safely ignored during binding
    [Fact]
    public void Bind_WhenUnknownParamsAreProvided_ShouldIgnoreThem()
    {
        var result = _binder.Bind<TestWidgetParams>(CreateContext("test?id=1&unknown=val"));

        result.Id.Should().Be(1);
    }

    // verifies that binding fails and throws an exception with correct metadata when a parameter type mismatch is encountered
    [Fact]
    public void Bind_WhenTypeMismatchOccurs_ShouldThrowInvalidParameterTypeException()
    {
        var act = () => _binder.Bind<TestWidgetParams>(CreateContext("test?id=not-a-number"));

        act.Should().Throw<InvalidParameterTypeException>()
            .Which.ParameterName.Should().Be("Id");
    }

    // verifies that the thrown exception contains the raw alias parameter name when its type matching fails
    [Fact]
    public void Bind_WhenAliasTypeMismatchOccurs_ShouldThrowInvalidParameterTypeExceptionWithAliasName()
    {
        var act = () => _binder.Bind<TestWidgetParams>(CreateContext("test?a=invalid-bool"));

        act.Should().Throw<InvalidParameterTypeException>()
            .Which.ParameterName.Should().Be("a");
    }

    // verifies that empty query parameter values are mapped to empty strings for string properties
    [Fact]
    public void Bind_WhenEmptyStringValueIsProvided_ShouldMapToEmptyString()
    {
        var result = _binder.Bind<TestWidgetParams>(CreateContext("test?s="));

        result.Status.Should().Be(string.Empty);
    }

    // verifies that attribute names are matched case-insensitively
    [Fact]
    public void Bind_WhenAttributeNameDiffersInCase_ShouldMapToModel()
    {
        var result = _binder.Bind<TestWidgetParams>(CreateContext("test?S=ok"));

        result.Status.Should().Be("ok");
    }

    // verifies that percent-escaped values are unescaped, as in callback data
    [Fact]
    public void Bind_WhenValueIsEscaped_ShouldUnescapeIt()
    {
        var result = _binder.Bind<TestWidgetParams>(CreateContext("test?s=a%26b%3Dc"));

        result.Status.Should().Be("a&b=c");
    }

    // verifies that a required parameter missing from the query data is reported
    [Theory]
    [InlineData("test")]
    [InlineData("test?other=1")]
    public void Bind_WhenRequiredParameterIsMissing_ShouldThrowMissingParameterException(string data)
    {
        var act = () => _binder.Bind<RequiredWidgetParams>(CreateContext(data));

        act.Should().Throw<MissingParameterException>().Which.ParameterName.Should().Be("Id");
    }

    // verifies that a provided required parameter is bound
    [Fact]
    public void Bind_WhenRequiredParameterIsProvided_ShouldMapToModel()
    {
        var result = _binder.Bind<RequiredWidgetParams>(CreateContext("test?id=5"));

        result.Id.Should().Be(5);
    }

    // verifies that models without parameters ignore any query string
    [Fact]
    public void Bind_WhenModelHasNoParameters_ShouldIgnoreQueryString()
    {
        var result = _binder.Bind<EmptyWidgetQueryParams>(CreateContext("test?a=1&b=2"));

        result.Should().NotBeNull();
    }

    // verifies that duplicate parameter names are rejected, names being case-insensitive
    [Fact]
    public void Bind_WhenModelDeclaresDuplicateNames_ShouldThrowInvalidOperationException()
    {
        var act = () => _binder.Bind<DuplicateNameWidgetParams>(CreateContext("test?x=1"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*more than once*");
    }

    // verifies that the data of a forwarded copy is bound exactly like the data of an original
    [Fact]
    public void Bind_WhenContextIsCopy_ShouldMapToModel()
    {
        var context = new WidgetQueryContext(WidgetQueryTestData.Copy("test?id=42&s=ok"));

        var result = _binder.Bind<TestWidgetParams>(context);

        result.Id.Should().Be(42);
        result.Status.Should().Be("ok");
    }

    // verifies that the data of a message sent via the bot is bound exactly like the data of an original
    [Fact]
    public void Bind_WhenContextIsViaBot_ShouldMapToModel()
    {
        var context = new WidgetQueryContext(WidgetQueryTestData.ViaBot("test?id=7"));

        var result = _binder.Bind<TestWidgetParams>(context);

        result.Id.Should().Be(7);
    }
}
