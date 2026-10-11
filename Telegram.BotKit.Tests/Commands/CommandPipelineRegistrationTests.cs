using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandPipelineRegistrationTests
{
    private sealed class StartHandler : ICommandHandler<EmptyCommandParams>
    {
        public static string Command => "start";

        public Task HandleAsync(EmptyCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class HelpHandler : ICommandHandler<EmptyCommandParams>
    {
        public static string Command => "help";

        public Task HandleAsync(EmptyCommandParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that handlers sharing identical parameter models register distinct invoker types without di collisions
    [Fact]
    public void AddHandler_WhenMultipleHandlersShareSameParameters_ShouldRegisterDistinctInvokers()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ICommandParameterBinder>());

        var builder = new CommandPipelineBuilder(services);

        // act
        builder.AddHandler<StartHandler>();
        builder.AddHandler<HelpHandler>();

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<CommandHandlerRegistry>();

        // assert
        registry.TryGetInvokerType("start", out var startInvokerType).Should().BeTrue();
        registry.TryGetInvokerType("help", out var helpInvokerType).Should().BeTrue();

        startInvokerType.Should().Be(typeof(CommandHandlerInvoker<StartHandler, EmptyCommandParams>));
        helpInvokerType.Should().Be(typeof(CommandHandlerInvoker<HelpHandler, EmptyCommandParams>));

        var startInvoker = (ICommandHandlerInvoker)provider.GetRequiredService(startInvokerType!);
        var helpInvoker = (ICommandHandlerInvoker)provider.GetRequiredService(helpInvokerType!);

        startInvoker.HandlerType.Should().Be(typeof(StartHandler));
        helpInvoker.HandlerType.Should().Be(typeof(HelpHandler));
    }
}