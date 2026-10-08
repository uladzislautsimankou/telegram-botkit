using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackRoutingMiddlewareTests
{
    private IServiceProvider CreateMockServiceProvider(Dictionary<Type, object> services)
    {
        var provider = Substitute.For<IServiceProvider>();
        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        provider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);

        foreach (var (type, instance) in services)
        {
            provider.GetService(type).Returns(instance);
        }

        return provider;
    }

    // verifies that a callback with an exact key match assigns the matched invoker to the context and continues the pipeline
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatchesExactly_ShouldAssignInvokerAndCallNext()
    {
        // arrange
        var invoker = Substitute.For<ICallbackHandlerInvoker>();
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
    }

    // verifies that a callback key containing parameters is successfully resolved and matched
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyMatchesWithParameters_ShouldAssignInvokerAndCallNext()
    {
        // arrange
        var invoker = Substitute.For<ICallbackHandlerInvoker>();
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
    }

    // verifies that an unknown callback trigger throws a route not found exception
    [Fact]
    public async Task InvokeAsync_WhenCallbackKeyIsUnknown_ShouldThrowRouteNotFoundException()
    {
        // arrange
        var invoker = Substitute.For<ICallbackHandlerInvoker>();
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