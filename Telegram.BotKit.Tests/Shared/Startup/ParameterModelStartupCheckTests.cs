using FluentAssertions;
using NSubstitute;
using Telegram.BotKit.Shared.Binding;
using Telegram.BotKit.Shared.Startup;

namespace Telegram.BotKit.Tests.Shared.Startup;

public sealed class ParameterModelStartupCheckTests
{
    private sealed class FirstHandler;

    private sealed class SecondHandler;

    private sealed class FirstParams;

    private sealed class SecondParams;

    private readonly IParameterModelValidator _validator = Substitute.For<IParameterModelValidator>();
    private readonly IServiceProvider _services = Substitute.For<IServiceProvider>();

    private ParameterModelStartupCheck CreateCheck(params (Type HandlerType, Type ParamsType)[] models) =>
        new("Command", () => models, _ => _validator);

    // verifies that valid models produce no problems
    [Fact]
    public void Validate_WhenAllModelsAreValid_ShouldReturnNoProblems()
    {
        _validator.Validate(Arg.Any<Type>()).Returns([]);

        var problems = CreateCheck((typeof(FirstHandler), typeof(FirstParams))).Validate(_services);

        problems.Should().BeEmpty();
    }

    // verifies that every problem is attributed to the handler kind and the handler that uses the model
    [Fact]
    public void Validate_WhenModelsHaveProblems_ShouldPrefixThemWithHandlerKindAndName()
    {
        _validator.Validate(typeof(FirstParams)).Returns(["bad name"]);
        _validator.Validate(typeof(SecondParams)).Returns(["bad type", "bad position"]);

        var problems = CreateCheck(
            (typeof(FirstHandler), typeof(FirstParams)),
            (typeof(SecondHandler), typeof(SecondParams))).Validate(_services);

        problems.Should().Equal(
            "Command handler 'FirstHandler': bad name",
            "Command handler 'SecondHandler': bad type",
            "Command handler 'SecondHandler': bad position");
    }

    // verifies that handlers sharing one model are all reported, since each of them is affected
    [Fact]
    public void Validate_WhenHandlersShareInvalidModel_ShouldReportEachHandler()
    {
        _validator.Validate(typeof(FirstParams)).Returns(["bad name"]);

        var problems = CreateCheck(
            (typeof(FirstHandler), typeof(FirstParams)),
            (typeof(SecondHandler), typeof(FirstParams))).Validate(_services);

        problems.Should().HaveCount(2);
    }
}
