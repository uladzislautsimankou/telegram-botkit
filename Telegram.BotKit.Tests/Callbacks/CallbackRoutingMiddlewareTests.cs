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

    private IServiceProvider CreateMockServiceProvider(Dictionary<Type, object> services)
    {
        var provider = Substitute.For<IServiceProvider>();

        foreach (var (type, instance) in services)
        {
            provider.GetService(type).Returns(instance);
        }

        return provider;
    }

    private (ICallbackHandlerInvoker Invoker, DummyHandler Handler) CreateMockInvoker()
    {
        var handler = new DummyHandler();
        var invoker = Substitute.For<ICallbackHandlerInvoker>();
        invoker.HandlerType.Returns(typeof(DummyHandler));
        invoker.HandlerInstance.Returns(handler);

        return (invoker, handler);
    }

    // verifies that a callback with an exact key match assigns the matched invoker and metadata to the context and continues the pipeline
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatchesExactly_ShouldAssignInvokerAndMetadataAndCallNext()
    {
        // arrange
        var (invoker, handler) = CreateMockInvoker();
        var registry = new CallbackHandlerRegistry();
        registry.Register("menu:main", typeof(ICallbackHandlerInvoker));

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(ICallbackHandlerInvoker)] = invoker
        });

        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider);
        var context = CreateCallbackContext("menu:main");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedInvoker.Should().BeSameAs(invoker);
        context.MatchedCallbackMetadata.Should().NotBeNull();
        context.MatchedCallbackMetadata!.HandlerType.Should().Be(typeof(DummyHandler));
        context.MatchedCallbackMetadata.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that a callback key containing parameters is successfully resolved and metadata is populated
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatchesWithParameters_ShouldAssignInvokerAndMetadataAndCallNext()
    {
        // arrange
        var (invoker, handler) = CreateMockInvoker();
        var registry = new CallbackHandlerRegistry();
        registry.Register("menu:main", typeof(ICallbackHandlerInvoker));

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(ICallbackHandlerInvoker)] = invoker
        });

        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider);
        var context = CreateCallbackContext("menu:main?id=123");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedInvoker.Should().BeSameAs(invoker);
        context.MatchedCallbackMetadata.Should().NotBeNull();
        context.MatchedCallbackMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that an unknown callback trigger throws a route not found exception
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyIsUnknown_ShouldThrowRouteNotFoundException()
    {
        // arrange
        var (invoker, _) = CreateMockInvoker();
        var registry = new CallbackHandlerRegistry();
        registry.Register("menu:main", typeof(ICallbackHandlerInvoker));

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(ICallbackHandlerInvoker)] = invoker
        });

        var middleware = new CallbackRoutingMiddleware(registry, serviceProvider);
        var context = CreateCallbackContext("unknown:action");

        // act
        var act = () => middleware.InvokeAsync(context, () => Task.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<RouteNotFoundException>();
    }

    private CallbackContext CreateCallbackContext(string data)
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