using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackRoutingMiddlewareTests
{
    private sealed class DummyHandler;

    private static (CallbackRegistration Registration, DummyHandler Handler) CreateMockRegistration()
    {
        var handler = new DummyHandler();
        var registration = new CallbackRegistration(
            HandlerType: typeof(DummyHandler),
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
        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider);
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
        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider);
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
        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider);
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

    // verifies that an unregistered callback key throws a route not found exception
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyIsUnknown_ShouldThrowRouteNotFoundException()
    {
        // arrange
        var (registration, _) = CreateMockRegistration();
        var registry = new CallbackHandlerRegistry();
        registry.Register("menu:main", registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider);
        var context = CreateCallbackContext("unknown:action");

        // act
        var act = async () => await middleware.InvokeAsync(context, static () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<RouteNotFoundException>();
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
}