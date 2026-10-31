using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;

namespace Telegram.BotKit.Tests.Messages;

public sealed class MessagePipelineRegistrationTests
{
    private sealed class OrderedHandler : IMessageHandler
    {
        public static int Order => 5;
        public static bool CanHandle(MessageContext context) => true;

        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

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
        var registry = new MessageHandlerRegistry();
        var builder = new MessagePipelineBuilder(services, registry);

        // act
        builder.AddHandler<StateBoundMessageHandler>();
        builder.AddHandler<GlobalMessageHandler>();

        await using var provider = services.BuildServiceProvider();
        registry.Build();

        // assert
        registry.GetHandlersForState("registration").ToArray().Should().ContainSingle().Which.HandlerType.Should().Be(typeof(StateBoundMessageHandler));
        registry.GetGlobalHandlers().ToArray().Should().ContainSingle().Which.HandlerType.Should().Be(typeof(GlobalMessageHandler));

        provider.GetRequiredService<StateBoundMessageHandler>().Should().NotBeNull();
        provider.GetRequiredService<GlobalMessageHandler>().Should().NotBeNull();
    }

    // verifies that duplicate message handler registrations are ignored by the registry and service collection
    [Fact]
    public async Task AddHandler_WhenSameHandlerAddedTwice_ShouldNotDuplicateInRegistryOrDi()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new MessageHandlerRegistry();
        var builder = new MessagePipelineBuilder(services, registry);

        // act
        builder.AddHandler<GlobalMessageHandler>();
        builder.AddHandler<GlobalMessageHandler>();

        await using var provider = services.BuildServiceProvider();
        registry.Build();

        // assert
        registry.GetGlobalHandlers().ToArray().Should().ContainSingle().Which.HandlerType.Should().Be(typeof(GlobalMessageHandler));
        provider.GetServices<GlobalMessageHandler>().Should().ContainSingle();
    }

    // verifies that Order declared by the handler is stored in its descriptor, also on the scanning path
    [Fact]
    public void RegisterHandlerByType_WhenHandlerDeclaresOrder_ShouldStoreItInDescriptor()
    {
        var registry = new MessageHandlerRegistry();

        MessageRegistrationHelper.RegisterHandlerByType(new ServiceCollection(), registry, typeof(OrderedHandler));
        registry.Build();

        registry.GetGlobalHandlers().ToArray().Should().ContainSingle().Which.Order.Should().Be(5);
    }
}
