using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.BotKit.Hosting;

namespace Telegram.BotKit.Tests.Hosting;

public sealed class UpdateHandlerStartupCheckTests
{
    private interface IMissingDependency;

    private sealed class AvailableDependency : IMissingDependency;

    private sealed class HandlerWithDependency(IMissingDependency dependency) : IUpdateHandler
    {
        public IMissingDependency Dependency { get; } = dependency;

        public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private static IEnumerable<string> Run(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        using var provider = services.BuildServiceProvider();

        return new UpdateHandlerStartupCheck().Validate(provider).ToList();
    }

    // verifies that a missing handler is reported with a ready-to-use registration example
    [Fact]
    public void Validate_WhenUpdateHandlerIsNotRegistered_ShouldExplainHowToRegisterIt()
    {
        var problems = Run(_ => { });

        problems.Should().ContainSingle()
            .Which.Should().Contain("No IUpdateHandler is registered")
            .And.Contain("AddSingleton<IUpdateHandler, YourUpdateHandler>()")
            .And.Contain("ICommandDispatcher");
    }

    // verifies that the registration can be detected before the container is built
    [Fact]
    public void IsRegistered_ShouldReflectTheServiceCollection()
    {
        var services = new ServiceCollection();
        UpdateHandlerStartupCheck.IsRegistered(services).Should().BeFalse();

        services.AddSingleton<IUpdateHandler, HandlerWithDependency>();

        UpdateHandlerStartupCheck.IsRegistered(services).Should().BeTrue();
    }

    // verifies that the pre-build message tells how to fix the problem
    [Fact]
    public void NotRegisteredMessage_ShouldContainRegistrationExample()
    {
        UpdateHandlerStartupCheck.NotRegisteredMessage
            .Should().StartWith("No IUpdateHandler is registered")
            .And.Contain("AddSingleton<IUpdateHandler, YourUpdateHandler>()");
    }

    // verifies that a registered handler passes
    [Fact]
    public void Validate_WhenUpdateHandlerIsRegistered_ShouldReturnNoProblems()
    {
        var problems = Run(services => services.AddSingleton(Substitute.For<IUpdateHandler>()));

        problems.Should().BeEmpty();
    }

    // verifies that a handler whose dependencies are missing is reported too, naming the missing dependency
    [Fact]
    public void Validate_WhenUpdateHandlerDependencyIsMissing_ShouldReportProblem()
    {
        var problems = Run(services => services.AddSingleton<IUpdateHandler, HandlerWithDependency>());

        problems.Should().ContainSingle()
            .Which.Should().Contain("IUpdateHandler is registered, but it cannot be created")
            .And.Contain("IMissingDependency");
    }

    // verifies that the check works for a handler registered with its own dependencies available
    [Fact]
    public void Validate_WhenUpdateHandlerDependencyIsRegistered_ShouldReturnNoProblems()
    {
        var problems = Run(services =>
        {
            services.AddSingleton<IMissingDependency, AvailableDependency>();
            services.AddSingleton<IUpdateHandler, HandlerWithDependency>();
        });

        problems.Should().BeEmpty();
    }
}
