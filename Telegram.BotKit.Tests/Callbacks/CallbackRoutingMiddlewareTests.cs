using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackRoutingMiddlewareTests
{
    private sealed class DummyHandler;

    private readonly ILogger<CallbackRoutingMiddleware> _logger = Substitute.For<ILogger<CallbackRoutingMiddleware>>();

    private static (CallbackRegistration Registration, DummyHandler Handler) CreateMockRegistration()
    {
        var handler = new DummyHandler();
        var registration = new CallbackRegistration(
            HandlerType: typeof(DummyHandler),
            ParamsType: typeof(object),
            Factory: _ => handler,
            Invoker: static (_, _, _, _) => Task.CompletedTask
        );

        return (registration, handler);
    }

    // verifies that an exact callback key match assigns the matched registration and metadata to the context and invokes next
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatchesExactly_ShouldAssignRegistrationAndMetadataAndCallNext()
    {
        // arrange
        var (registration, handler) = CreateMockRegistration();
        var registry = new CallbackHandlerRegistry();
        registry.Register("menu:main", registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider, _logger);
        var context = CreateCallbackContext("menu:main");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedCallbackMetadata.Should().NotBeNull();
        context.MatchedCallbackMetadata!.HandlerType.Should().Be(typeof(DummyHandler));
        context.MatchedCallbackMetadata.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that callback keys match case insensitively
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatchesCaseInsensitively_ShouldAssignRegistrationAndMetadataAndCallNext()
    {
        // arrange
        var (registration, handler) = CreateMockRegistration();
        var registry = new CallbackHandlerRegistry();
        registry.Register("menu:main", registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider, _logger);
        var context = CreateCallbackContext("MENU:MAIN");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedCallbackMetadata.Should().NotBeNull();
        context.MatchedCallbackMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that a callback key containing query parameters is resolved properly and metadata is populated
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatchesWithParameters_ShouldAssignRegistrationAndMetadataAndCallNext()
    {
        // arrange
        var (registration, handler) = CreateMockRegistration();
        var registry = new CallbackHandlerRegistry();
        registry.Register("menu:main", registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider, _logger);
        var context = CreateCallbackContext("menu:main?id=123");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedCallbackMetadata.Should().NotBeNull();
        context.MatchedCallbackMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that an unregistered callback key (e.g. a button from before a deployment) does not throw:
    // the context stays unmatched, a warning is logged and the pipeline continues, like in the other routers
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyIsUnknown_ShouldLeaveContextUnmatchedLogWarningAndCallNext()
    {
        // arrange
        var (registration, _) = CreateMockRegistration();
        var registry = new CallbackHandlerRegistry();
        registry.Register("menu:main", registration);
        registry.Build();

        var middleware = new CallbackRoutingMiddleware(registry, Substitute.For<IServiceProvider>(), _logger);
        var context = CreateCallbackContext("unknown:action");

        var nextCalled = false;
        NextDelegate next = () => { nextCalled = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        nextCalled.Should().BeTrue();
        context.MatchedCallbackMetadata.Should().BeNull();
        context.MatchedRegistration.Should().BeNull();
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("unknown")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that a matched key is not reported as a problem
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatches_ShouldNotLogWarning()
    {
        // arrange
        var (registration, _) = CreateMockRegistration();
        var registry = new CallbackHandlerRegistry();
        registry.Register("menu:main", registration);
        registry.Build();

        var middleware = new CallbackRoutingMiddleware(registry, Substitute.For<IServiceProvider>(), _logger);

        // act
        await middleware.InvokeAsync(CreateCallbackContext("menu:main"), static () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static CallbackContext CreateCallbackContext(string data)
    {
        var query = new CallbackQuery
        {
            Id = "1",
            Data = data,
            From = new User { Id = 123, FirstName = "User" },
            Message = new Message { Chat = new Chat { Id = 456 } }
        };

        return new CallbackContext(query);
    }

    // verifies that routing does not create the handler, and that the instance is created once when requested
    [Fact]
    public async Task InvokeAsync_WhenCallbackMatches_ShouldCreateHandlerLazilyAndOnce()
    {
        // arrange
        var factoryCalls = 0;
        var registration = new CallbackRegistration(
            typeof(object),
            typeof(object),
            _ => { factoryCalls++; return new object(); },
            static (_, _, _, _) => Task.CompletedTask);

        var registry = new CallbackHandlerRegistry();
        registry.Register("test:key", registration);
        registry.Build();

        var middleware = new CallbackRoutingMiddleware(registry, Substitute.For<IServiceProvider>(), _logger);
        var context = new CallbackContext(new CallbackQuery { Id = "1", Data = "test:key" });

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);
        var callsAfterRouting = factoryCalls;
        var first = context.MatchedCallbackMetadata!.HandlerInstance;
        var second = context.MatchedCallbackMetadata.HandlerInstance;

        // assert
        callsAfterRouting.Should().Be(0);
        factoryCalls.Should().Be(1);
        second.Should().BeSameAs(first);
    }
}
