using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.BotKit.Shared.Exceptions;
using Telegram.BotKit.Shared.Startup;

namespace Telegram.BotKit.Tests.Shared.Startup;

public sealed class StartupValidatorTests
{
    private sealed class FakeCheck(params string[] problems) : IStartupCheck
    {
        public IEnumerable<string> Validate(IServiceProvider services) => problems;
    }

    private static ServiceProvider BuildProvider(params IStartupCheck[] checks)
    {
        var services = new ServiceCollection();
        foreach (var check in checks)
        {
            services.AddSingleton(check);
        }

        return services.BuildServiceProvider();
    }

    // verifies that a valid setup passes silently
    [Fact]
    public void Run_WhenNoCheckReportsProblems_ShouldNotThrow()
    {
        using var provider = BuildProvider(new FakeCheck(), new FakeCheck());

        var act = () => StartupValidator.Run(provider);

        act.Should().NotThrow();
    }

    // verifies that the absence of any registered check is a valid setup
    [Fact]
    public void Run_WhenNoChecksAreRegistered_ShouldNotThrow()
    {
        using var provider = BuildProvider();

        var act = () => StartupValidator.Run(provider);

        act.Should().NotThrow();
    }

    // verifies that problems of all checks are reported together, numbered, in one exception
    [Fact]
    public void Run_WhenChecksReportProblems_ShouldThrowWithAllProblemsNumbered()
    {
        using var provider = BuildProvider(new FakeCheck("first problem"), new FakeCheck(), new FakeCheck("second problem", "third problem"));

        var act = () => StartupValidator.Run(provider);

        var message = act.Should().Throw<BotStartupException>().Which.Message;
        message.Should().Contain("3 problem(s)")
            .And.Contain("1. first problem")
            .And.Contain("2. second problem")
            .And.Contain("3. third problem");
    }

    // verifies that the problems are written to the log as one critical entry before the exception is thrown
    [Fact]
    public void Run_WhenChecksReportProblems_ShouldLogCriticalOnce()
    {
        var logger = Substitute.For<ILogger>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger("Telegram.BotKit.Startup").Returns(logger);

        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        services.AddSingleton<IStartupCheck>(new FakeCheck("broken setup"));
        using var provider = services.BuildServiceProvider();

        var act = () => StartupValidator.Run(provider);

        act.Should().Throw<BotStartupException>();
        logger.Received(1).Log(
            LogLevel.Critical,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that a host without logging still gets the exception
    [Fact]
    public void Run_WhenLoggingIsNotConfigured_ShouldStillThrow()
    {
        using var provider = BuildProvider(new FakeCheck("broken setup"));

        var act = () => StartupValidator.Run(provider);

        act.Should().Throw<BotStartupException>().WithMessage("*broken setup*");
    }
}
