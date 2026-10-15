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

        public static bool CanHandle(MessageContext context) => true;

        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class GlobalMessageHandler : IMessageHandler
    {
        public static bool CanHandle(MessageContext context) => true;

        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that message handlers are registered in the registry grouped by their state requirements
    [Fact]
    public async Task AddHandler_WhenHandlersAreRegistered_ShouldCategorizeInRegistryCorrectly()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new MessagePipelineBuilder(services);

        // act
        builder.AddHandler<StateBoundMessageHandler>();
        builder.AddHandler<GlobalMessageHandler>();

        await using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<MessageHandlerRegistry>();
        registry.Build();

        // assert
        registry.GetHandlersForState("registration").ToArray().Should().ContainSingle().Which.HandlerType.Should().Be(typeof(StateBoundMessageHandler));
        registry.GetGlobalHandlers().ToArray().Should().ContainSingle().Which.HandlerType.Should().Be(typeof(GlobalMessageHandler));

        provider.GetRequiredService<StateBoundMessageHandler>().Should().NotBeNull();
        provider.GetRequiredService<GlobalMessageHandler>().Should().NotBeNull();
    }

    // verifies that duplicate message handler registrations are ignored by the registry
    [Fact]
    public async Task AddHandler_WhenSameHandlerAddedTwice_ShouldNotDuplicateInRegistry()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new MessagePipelineBuilder(services);

        // act
        builder.AddHandler<GlobalMessageHandler>();
        builder.AddHandler<GlobalMessageHandler>();

        await using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<MessageHandlerRegistry>();
        registry.Build();

        // assert
        registry.GetGlobalHandlers().ToArray().Should().ContainSingle().Which.HandlerType.Should().Be(typeof(GlobalMessageHandler));
    }

    // verifies that duplicate message handler registrations are ignored by the registry and service collection
    [Fact]
    public async Task AddHandler_WhenSameHandlerAddedTwice_ShouldNotDuplicateInRegistryOrDi()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new MessagePipelineBuilder(services);

        // act
        builder.AddHandler<GlobalMessageHandler>();
        builder.AddHandler<GlobalMessageHandler>();

        await using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<MessageHandlerRegistry>();
        registry.Build();

        // assert
        registry.GetGlobalHandlers().ToArray().Should().ContainSingle().Which.HandlerType.Should().Be(typeof(GlobalMessageHandler));
        provider.GetServices<GlobalMessageHandler>().Should().ContainSingle();
    }
}
