using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Messages;

public sealed class MessageRoutingMiddlewareTests
{
    private sealed class TestStateHandler(bool canHandle) : IMessageHandler
    {
        public static string? RequiredState => "waiting_for_name";

        public bool CanHandle(MessageContext context) => canHandle;

        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestGlobalHandler(bool canHandle) : IMessageHandler
    {
        public bool CanHandle(MessageContext context) => canHandle;

        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    // verifies that a state-specific handler is preferred and routed to when the user is in that state
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsSetAndMatchingStateHandlerExists_ShouldSetMatchedHandlerAndCallNext()
    {
        // arrange
        var stateHandler = new TestStateHandler(canHandle: true);
        var globalHandler = new TestGlobalHandler(canHandle: true);

        var registry = new MessageHandlerRegistry();
        registry.Register(typeof(TestStateHandler), "waiting_for_name");
        registry.Register(typeof(TestGlobalHandler), null);
        registry.Build();

        var services = new ServiceCollection();
        services.AddSingleton(stateHandler);
        services.AddSingleton(globalHandler);
        await using var provider = services.BuildServiceProvider();

        var middleware = new MessageRoutingMiddleware(registry, provider);
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            UserState = "waiting_for_name"
        };
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedHandler.Should().BeSameAs(stateHandler);
        context.MatchedMessageMetadata.Should().NotBeNull();
        context.MatchedMessageMetadata!.HandlerType.Should().Be(typeof(TestStateHandler));
        context.MatchedMessageMetadata.HandlerInstance.Should().BeSameAs(stateHandler);
    }

    // verifies that user state matches registered state case insensitively
    [Fact]
    public async Task InvokeAsync_WhenUserStateMatchesCaseInsensitively_ShouldSetMatchedHandlerAndCallNext()
    {
        // arrange
        var stateHandler = new TestStateHandler(canHandle: true);

        var registry = new MessageHandlerRegistry();
        registry.Register(typeof(TestStateHandler), "waiting_for_name");
        registry.Build();

        var services = new ServiceCollection();
        services.AddSingleton(stateHandler);
        await using var provider = services.BuildServiceProvider();

        var middleware = new MessageRoutingMiddleware(registry, provider);
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            UserState = "WAITING_FOR_NAME"
        };
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedHandler.Should().BeSameAs(stateHandler);
    }

    // verifies that routing falls back to a global handler when the user is in a state but no state-specific handler can handle the context
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsSetButNoStateHandlerCanHandle_ShouldFallbackToGlobalHandlerAndCallNext()
    {
        // arrange
        var stateHandler = new TestStateHandler(canHandle: false);
        var globalHandler = new TestGlobalHandler(canHandle: true);

        var registry = new MessageHandlerRegistry();
        registry.Register(typeof(TestStateHandler), "waiting_for_name");
        registry.Register(typeof(TestGlobalHandler), null);
        registry.Build();

        var services = new ServiceCollection();
        services.AddSingleton(stateHandler);
        services.AddSingleton(globalHandler);
        await using var provider = services.BuildServiceProvider();

        var middleware = new MessageRoutingMiddleware(registry, provider);
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            UserState = "waiting_for_name"
        };
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedHandler.Should().BeSameAs(globalHandler);
        context.MatchedMessageMetadata.Should().NotBeNull();
        context.MatchedMessageMetadata!.HandlerType.Should().Be(typeof(TestGlobalHandler));
        context.MatchedMessageMetadata.HandlerInstance.Should().BeSameAs(globalHandler);
    }

    // verifies that a global handler is mapped when the user has no active state
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsNullAndGlobalHandlerMatches_ShouldSetMatchedHandlerAndCallNext()
    {
        // arrange
        var stateHandler = new TestStateHandler(canHandle: true);
        var globalHandler = new TestGlobalHandler(canHandle: true);

        var registry = new MessageHandlerRegistry();
        registry.Register(typeof(TestStateHandler), "waiting_for_name");
        registry.Register(typeof(TestGlobalHandler), null);
        registry.Build();

        var services = new ServiceCollection();
        services.AddSingleton(stateHandler);
        services.AddSingleton(globalHandler);
        await using var provider = services.BuildServiceProvider();

        var middleware = new MessageRoutingMiddleware(registry, provider);
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            UserState = null
        };
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedHandler.Should().BeSameAs(globalHandler);
        context.MatchedMessageMetadata.Should().NotBeNull();
        context.MatchedMessageMetadata!.HandlerInstance.Should().BeSameAs(globalHandler);
    }

    // verifies that whitespace user state is treated as absent state and routes to a global handler
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsWhitespace_ShouldTreatAsNoStateAndFallbackToGlobalHandler()
    {
        // arrange
        var globalHandler = new TestGlobalHandler(canHandle: true);

        var registry = new MessageHandlerRegistry();
        registry.Register(typeof(TestGlobalHandler), null);
        registry.Build();

        var services = new ServiceCollection();
        services.AddSingleton(globalHandler);
        await using var provider = services.BuildServiceProvider();

        var middleware = new MessageRoutingMiddleware(registry, provider);
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            UserState = "   "
        };
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedHandler.Should().BeSameAs(globalHandler);
    }

    // verifies that no handler is matched and the pipeline is not executed further when no handler criteria are met
    [Fact]
    public async Task InvokeAsync_WhenNoHandlersCanHandleContext_ShouldNotSetMatchedHandlerAndShouldNotCallNext()
    {
        // arrange
        var globalHandler = new TestGlobalHandler(canHandle: false);

        var registry = new MessageHandlerRegistry();
        registry.Register(typeof(TestGlobalHandler), null);
        registry.Build();

        var services = new ServiceCollection();
        services.AddSingleton(globalHandler);
        await using var provider = services.BuildServiceProvider();

        var middleware = new MessageRoutingMiddleware(registry, provider);
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
        context.MatchedHandler.Should().BeNull();
        context.MatchedMessageMetadata.Should().BeNull();
    }
}