using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries.Invocation;
using Telegram.BotKit.WidgetQueries.Models;
using Telegram.BotKit.WidgetQueries.Routing;

namespace Telegram.BotKit.Tests.WidgetQueries;

public sealed class WidgetQueryRoutingMiddlewareTests
{
    private sealed class DummyHandler;

    private readonly ILogger<WidgetQueryRoutingMiddleware> _logger = Substitute.For<ILogger<WidgetQueryRoutingMiddleware>>();

    private static (WidgetQueryRegistration Registration, DummyHandler Handler) CreateMockRegistration()
    {
        var handler = new DummyHandler();
        var registration = new WidgetQueryRegistration(
            HandlerType: typeof(DummyHandler),
            ParamsType: typeof(object),
            Factory: _ => handler,
            Invoker: static (_, _, _, _) => Task.CompletedTask
        );

        return (registration, handler);
    }

    private WidgetQueryRoutingMiddleware CreateMiddleware(WidgetQueryHandlerRegistry registry) =>
        new(registry, Substitute.For<IServiceProvider>(), _logger);

    // verifies that an exact route key match assigns the matched registration and metadata to the context and invokes next
    [Fact]
    public async Task InvokeAsync_WhenKeyMatchesExactly_ShouldAssignRegistrationAndMetadataAndCallNext()
    {
        // arrange
        var (registration, handler) = CreateMockRegistration();
        var registry = new WidgetQueryHandlerRegistry();
        registry.Register("poll:vote", registration);
        registry.Build();

        var middleware = CreateMiddleware(registry);
        var context = WidgetQueryTestData.Context("poll:vote");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedWidgetQueryMetadata.Should().NotBeNull();
        context.MatchedWidgetQueryMetadata!.HandlerType.Should().Be(typeof(DummyHandler));
        context.MatchedWidgetQueryMetadata.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that route keys match case insensitively
    [Fact]
    public async Task InvokeAsync_WhenKeyMatchesCaseInsensitively_ShouldAssignRegistrationAndMetadataAndCallNext()
    {
        // arrange
        var (registration, handler) = CreateMockRegistration();
        var registry = new WidgetQueryHandlerRegistry();
        registry.Register("poll:vote", registration);
        registry.Build();

        var middleware = CreateMiddleware(registry);
        var context = WidgetQueryTestData.Context("POLL:VOTE");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedWidgetQueryMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that a key followed by query parameters is resolved properly and metadata is populated
    [Fact]
    public async Task InvokeAsync_WhenKeyMatchesWithParameters_ShouldAssignRegistrationAndMetadataAndCallNext()
    {
        // arrange
        var (registration, handler) = CreateMockRegistration();
        var registry = new WidgetQueryHandlerRegistry();
        registry.Register("poll:vote", registration);
        registry.Build();

        var middleware = CreateMiddleware(registry);
        var context = WidgetQueryTestData.Context("poll:vote?o=2");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedWidgetQueryMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that a query of a forwarded copy is routed by the same key as a query of an original message
    [Fact]
    public async Task InvokeAsync_WhenContextIsCopy_ShouldRouteByTheSameKey()
    {
        // arrange
        var (registration, _) = CreateMockRegistration();
        var registry = new WidgetQueryHandlerRegistry();
        registry.Register("poll:vote", registration);
        registry.Build();

        var middleware = CreateMiddleware(registry);
        var context = new WidgetQueryContext(WidgetQueryTestData.Copy("poll:vote?o=1"));

        // act
        await middleware.InvokeAsync(context, static () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        context.IsCopy.Should().BeTrue();
        context.MatchedRegistration.Should().BeSameAs(registration);
    }

    // verifies that an unregistered key (e.g. a widget from before a deployment) does not throw:
    // the context stays unmatched, a warning is logged and the pipeline continues, like in the other routers
    [Fact]
    public async Task InvokeAsync_WhenKeyIsUnknown_ShouldLeaveContextUnmatchedLogWarningAndCallNext()
    {
        // arrange
        var (registration, _) = CreateMockRegistration();
        var registry = new WidgetQueryHandlerRegistry();
        registry.Register("poll:vote", registration);
        registry.Build();

        var middleware = CreateMiddleware(registry);
        var context = WidgetQueryTestData.Context("unknown:action");

        var nextCalled = false;
        NextDelegate next = () => { nextCalled = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        nextCalled.Should().BeTrue();
        context.MatchedWidgetQueryMetadata.Should().BeNull();
        context.MatchedRegistration.Should().BeNull();
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("unknown:action")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that a matched key is not reported as a problem
    [Fact]
    public async Task InvokeAsync_WhenKeyMatches_ShouldNotLogWarning()
    {
        // arrange
        var (registration, _) = CreateMockRegistration();
        var registry = new WidgetQueryHandlerRegistry();
        registry.Register("poll:vote", registration);
        registry.Build();

        var middleware = CreateMiddleware(registry);

        // act
        await middleware.InvokeAsync(WidgetQueryTestData.Context("poll:vote"), static () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that routing does not create the handler, and that the instance is created once when requested
    [Fact]
    public async Task InvokeAsync_WhenKeyMatches_ShouldCreateHandlerLazilyAndOnce()
    {
        // arrange
        var factoryCalls = 0;
        var registration = new WidgetQueryRegistration(
            typeof(object),
            typeof(object),
            _ => { factoryCalls++; return new object(); },
            static (_, _, _, _) => Task.CompletedTask);

        var registry = new WidgetQueryHandlerRegistry();
        registry.Register("test:key", registration);
        registry.Build();

        var middleware = CreateMiddleware(registry);
        var context = WidgetQueryTestData.Context("test:key");

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);
        var callsAfterRouting = factoryCalls;
        var first = context.MatchedWidgetQueryMetadata!.HandlerInstance;
        var second = context.MatchedWidgetQueryMetadata.HandlerInstance;

        // assert
        callsAfterRouting.Should().Be(0);
        factoryCalls.Should().Be(1);
        second.Should().BeSameAs(first);
    }
}
