using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandPipelineRegistrationTests
{
    private sealed class StartHandler : ICommandHandler<EmptyCommandParams>
    {
        public static string Command => "start";
        public static string[] Aliases => ["s", "begin"];

        public Task HandleAsync(EmptyCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class HelpHandler : ICommandHandler<EmptyCommandParams>
    {
        public static string Command => "help";

        public Task HandleAsync(EmptyCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that handlers sharing identical parameter models register distinct registrations and factories without collisions
    [Fact]
    public async Task AddHandler_WhenMultipleHandlersShareSameParameters_ShouldRegisterDistinctRegistrations()
    {
        // arrange
        var services = new ServiceCollection();

        var registry = new CommandHandlerRegistry();
        var builder = new CommandPipelineBuilder(services, registry);

        // act
        builder.AddHandler<StartHandler>();
        builder.AddHandler<HelpHandler>();

        await using var provider = services.BuildServiceProvider();
        registry.Build();

        // assert
        registry.TryGetRegistration("start", out var startRegistration).Should().BeTrue();
        registry.TryGetRegistration("help", out var helpRegistration).Should().BeTrue();

        startRegistration!.HandlerType.Should().Be(typeof(StartHandler));
        helpRegistration!.HandlerType.Should().Be(typeof(HelpHandler));

        var startInstance = startRegistration.Factory(provider);
        var helpInstance = helpRegistration.Factory(provider);

        startInstance.Should().BeOfType<StartHandler>();
        helpInstance.Should().BeOfType<HelpHandler>();
    }

    // verifies that command handlers register their trigger and all declared aliases
    [Fact]
    public async Task AddHandler_WhenHandlerHasAliases_ShouldRegisterCommandAndAllAliasesInRegistry()
    {
        // arrange
        var services = new ServiceCollection();

        var registry = new CommandHandlerRegistry();
        var builder = new CommandPipelineBuilder(services, registry);

        // act
        builder.AddHandler<StartHandler>();

        await using var provider = services.BuildServiceProvider();
        registry.Build();

        // assert
        registry.TryGetRegistration("start", out var registration).Should().BeTrue();
        registration.Should().NotBeNull();
        registry.TryGetAliasRegistration("s", out var firstAlias).Should().BeTrue();
        firstAlias.Should().NotBeNull();
        registry.TryGetAliasRegistration("begin", out var secondAlias).Should().BeTrue();
        secondAlias.Should().NotBeNull();
    }

    // verifies that multiple calls to userouting do not introduce duplicate steps
    [Fact]
    public void UseRouting_WhenCalledMultipleTimes_ShouldRegisterOnlySingleStep()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new CommandPipelineBuilder(services, new CommandHandlerRegistry());

        // act
        builder.UseRouting();
        builder.UseRouting();

        // assert
        builder.Steps.Should().ContainSingle(step => step.Type == PipelineStepType.Routing);
    }
}
