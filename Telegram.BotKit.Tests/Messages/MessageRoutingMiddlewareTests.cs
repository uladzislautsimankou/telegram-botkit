using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.Messages;

public sealed class MessageRoutingMiddlewareTests
{
    private sealed class MatchingStateHandler : IMessageHandler
    {
        public static string? RequiredState => "waiting_for_name";
        public static bool CanHandle(MessageContext context) => true;
        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NonMatchingStateHandler : IMessageHandler
    {
        public static string? RequiredState => "waiting_for_name";
        public static bool CanHandle(MessageContext context) => false;
        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MatchingGlobalHandler : IMessageHandler
    {
        public static bool CanHandle(MessageContext context) => true;
        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NonMatchingGlobalHandler : IMessageHandler
    {
        public static bool CanHandle(MessageContext context) => false;
        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static MessageHandlerDescriptor CreateDescriptor<THandler>()
        where THandler : class, IMessageHandler
    {
        return new MessageHandlerDescriptor(
            typeof(THandler),
            THandler.CanHandle,
            static sp => sp.GetRequiredService<THandler>());
    }

    private sealed class AnotherMatchingGlobalHandler : IMessageHandler
    {
        public static bool CanHandle(MessageContext context) => true;
        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static object? Matched(MessageContext context) => context.MatchedMessageMetadata?.HandlerInstance;

    // verifies that a state-specific handler is preferred and routed to when the user is in that state
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsSetAndMatchingStateHandlerExists_ShouldSetMatchedHandlerAndCallNext()
    {
        // arrange
        var stateHandler = new MatchingStateHandler();
        var globalHandler = new MatchingGlobalHandler();

        var registry = new MessageHandlerRegistry();
        registry.Register(CreateDescriptor<MatchingStateHandler>(), "waiting_for_name");
        registry.Register(CreateDescriptor<MatchingGlobalHandler>(), null);
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
        Matched(context).Should().BeSameAs(stateHandler);
        context.MatchedMessageMetadata.Should().NotBeNull();
        context.MatchedMessageMetadata!.HandlerType.Should().Be(typeof(MatchingStateHandler));
        context.MatchedMessageMetadata.HandlerInstance.Should().BeSameAs(stateHandler);
    }

    // verifies that the user state is matched case-insensitively and ignoring surrounding whitespace
    [Theory]
    [InlineData("WAITING_FOR_NAME")]
    [InlineData("  waiting_for_name  ")]
    public async Task InvokeAsync_WhenUserStateDiffersInCaseOrPadding_ShouldSetMatchedHandlerAndCallNext(string userState)
    {
        // arrange
        var stateHandler = new MatchingStateHandler();

        var registry = new MessageHandlerRegistry();
        registry.Register(CreateDescriptor<MatchingStateHandler>(), "waiting_for_name");
        registry.Build();

        var services = new ServiceCollection();
        services.AddSingleton(stateHandler);
        await using var provider = services.BuildServiceProvider();

        var middleware = new MessageRoutingMiddleware(registry, provider);
        var context = new MessageContext(new Message { Id = 123, Chat = new Chat { Id = 1 } }) { UserState = userState };
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        Matched(context).Should().BeSameAs(stateHandler);
    }

    // verifies that routing falls back to a global handler when the user is in a state but no state-specific handler can handle the context
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsSetButNoStateHandlerCanHandle_ShouldFallbackToGlobalHandlerAndCallNext()
    {
        // arrange
        var globalHandler = new MatchingGlobalHandler();

        var registry = new MessageHandlerRegistry();
        registry.Register(CreateDescriptor<NonMatchingStateHandler>(), "waiting_for_name");
        registry.Register(CreateDescriptor<MatchingGlobalHandler>(), null);
        registry.Build();

        var services = new ServiceCollection();

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
        Matched(context).Should().BeSameAs(globalHandler);
        context.MatchedMessageMetadata.Should().NotBeNull();
        context.MatchedMessageMetadata!.HandlerType.Should().Be(typeof(MatchingGlobalHandler));
        context.MatchedMessageMetadata.HandlerInstance.Should().BeSameAs(globalHandler);
    }

    // verifies that a global handler is mapped when the user has no active state
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsNullAndGlobalHandlerMatches_ShouldSetMatchedHandlerAndCallNext()
    {
        // arrange
        var globalHandler = new MatchingGlobalHandler();

        var registry = new MessageHandlerRegistry();
        registry.Register(CreateDescriptor<MatchingStateHandler>(), "waiting_for_name");
        registry.Register(CreateDescriptor<MatchingGlobalHandler>(), null);
        registry.Build();

        var services = new ServiceCollection();
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
        Matched(context).Should().BeSameAs(globalHandler);
        context.MatchedMessageMetadata.Should().NotBeNull();
        context.MatchedMessageMetadata!.HandlerInstance.Should().BeSameAs(globalHandler);
    }

    // verifies that whitespace user state is treated as absent state and routes to a global handler
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsWhitespace_ShouldTreatAsNoStateAndFallbackToGlobalHandler()
    {
        // arrange
        var globalHandler = new MatchingGlobalHandler();

        var registry = new MessageHandlerRegistry();
        registry.Register(CreateDescriptor<MatchingGlobalHandler>(), null);
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
        Matched(context).Should().BeSameAs(globalHandler);
    }

    // verifies that the pipeline continues without a matched handler when no handler criteria are met
    [Fact]
    public async Task InvokeAsync_WhenNoHandlersCanHandleContext_ShouldNotSetMatchedHandlerButCallNext()
    {
        // arrange
        var registry = new MessageHandlerRegistry();
        registry.Register(CreateDescriptor<NonMatchingGlobalHandler>(), null);
        registry.Build();

        await using var provider = new ServiceCollection().BuildServiceProvider();
        var middleware = new MessageRoutingMiddleware(registry, provider);
        var context = new MessageContext(new Message { Id = 123, Chat = new Chat { Id = 1 } });
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        Matched(context).Should().BeNull();
        context.MatchedMessageMetadata.Should().BeNull();
    }

    // verifies that among several matching global handlers the first registered one wins
    [Fact]
    public async Task InvokeAsync_WhenSeveralGlobalHandlersMatch_ShouldPickFirstRegistered()
    {
        // arrange
        var first = new MatchingGlobalHandler();
        var second = new AnotherMatchingGlobalHandler();

        var registry = new MessageHandlerRegistry();
        registry.Register(CreateDescriptor<MatchingGlobalHandler>(), null);
        registry.Register(CreateDescriptor<AnotherMatchingGlobalHandler>(), null);
        registry.Build();

        var services = new ServiceCollection();
        services.AddSingleton(first);
        services.AddSingleton(second);
        await using var provider = services.BuildServiceProvider();

        var middleware = new MessageRoutingMiddleware(registry, provider);
        var context = new MessageContext(new Message { Id = 123, Chat = new Chat { Id = 1 } });

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        Matched(context).Should().BeSameAs(first);
    }

    // verifies that routing does not create the handler, and that the instance is created once when requested
    [Fact]
    public async Task InvokeAsync_WhenHandlerMatches_ShouldCreateItLazilyAndOnce()
    {
        // arrange
        var factoryCalls = 0;
        var registry = new MessageHandlerRegistry();
        registry.Register(new MessageHandlerDescriptor(
            typeof(MatchingGlobalHandler),
            MatchingGlobalHandler.CanHandle,
            _ => { factoryCalls++; return new MatchingGlobalHandler(); }), null);
        registry.Build();

        await using var provider = new ServiceCollection().BuildServiceProvider();
        var middleware = new MessageRoutingMiddleware(registry, provider);
        var context = new MessageContext(new Message { Id = 123, Chat = new Chat { Id = 1 } });

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);
        var callsAfterRouting = factoryCalls;
        var first = context.MatchedMessageMetadata!.HandlerInstance;
        var second = context.MatchedMessageMetadata.HandlerInstance;

        // assert
        callsAfterRouting.Should().Be(0);
        factoryCalls.Should().Be(1);
        second.Should().BeSameAs(first);
    }

    // verifies that a lower Order wins over registration order among matching handlers
    [Fact]
    public async Task InvokeAsync_WhenSeveralGlobalHandlersMatch_ShouldPickLowestOrder()
    {
        // arrange
        var registeredFirst = new MatchingGlobalHandler();
        var registeredSecond = new AnotherMatchingGlobalHandler();

        var registry = new MessageHandlerRegistry();
        registry.Register(new MessageHandlerDescriptor(
            typeof(MatchingGlobalHandler), MatchingGlobalHandler.CanHandle, _ => registeredFirst, Order: 10), null);
        registry.Register(new MessageHandlerDescriptor(
            typeof(AnotherMatchingGlobalHandler), AnotherMatchingGlobalHandler.CanHandle, _ => registeredSecond, Order: -5), null);
        registry.Build();

        await using var provider = new ServiceCollection().BuildServiceProvider();
        var middleware = new MessageRoutingMiddleware(registry, provider);
        var context = new MessageContext(new Message { Id = 123, Chat = new Chat { Id = 1 } });

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        Matched(context).Should().BeSameAs(registeredSecond);
    }
}
