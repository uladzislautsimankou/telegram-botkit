using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;

namespace Telegram.BotKit.Tests.Messages;

public sealed class MessagePipelineRegistrationTests
{
    private sealed class StateBoundMessageHandler : IMessageHandler
    {
        public static string? RequiredState => "registration";

        public bool CanHandle(MessageContext context) => true;

        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class GlobalMessageHandler : IMessageHandler
    {
        public bool CanHandle(MessageContext context) => true;

        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // verifies that message handlers are registered in the registry grouped by their state requirements
    [Fact]
    public void AddHandler_WhenHandlersAreRegistered_ShouldCategorizeInRegistryCorrectly()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new MessagePipelineBuilder(services);

        // act
        builder.AddHandler<StateBoundMessageHandler>();
        builder.AddHandler<GlobalMessageHandler>();

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<MessageHandlerRegistry>();

        // assert
        registry.GetHandlerTypesForState("registration").Should().ContainSingle().Which.Should().Be(typeof(StateBoundMessageHandler));
        registry.GetGlobalHandlerTypes().Should().ContainSingle().Which.Should().Be(typeof(GlobalMessageHandler));

        provider.GetRequiredService<StateBoundMessageHandler>().Should().NotBeNull();
        provider.GetRequiredService<GlobalMessageHandler>().Should().NotBeNull();
    }
}