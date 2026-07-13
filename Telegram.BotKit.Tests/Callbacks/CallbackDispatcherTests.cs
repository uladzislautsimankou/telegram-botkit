using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackDispatcherTests
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

    private sealed class DummyCallbackMiddleware : ICallbackMiddleware
    {
        public bool Called { get; private set; }

        public async Task InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Called = true;
            await next();
        }
    }

    private sealed class FaultyCallbackMiddleware : ICallbackMiddleware
    {
        public Task InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated pipeline failure");
        }
    }

    // verifies that a valid callback query successfully resolves and executes the registered middlewares in the pipeline
    [Fact]
    public async Task DispatchAsync_WhenQueryIsValid_ShouldBuildPipelineAndExecute()
    {
        // arrange
        var dummyMiddleware = new DummyCallbackMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new CallbackPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyCallbackMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(DummyCallbackMiddleware)] = dummyMiddleware
        });

        var dispatcher = new CallbackDispatcher(serviceProvider, pipelineBuilder);
        var query = new CallbackQuery
        {
            Id = "query_123",
            Data = "menu:main",
            From = new User { Id = 456, FirstName = "User" }
        };

        // act
        await dispatcher.DispatchAsync(query, TestContext.Current.CancellationToken);

        // assert
        dummyMiddleware.Called.Should().BeTrue();
    }

    // verifies that an exception thrown inside a middleware bubbles up directly through the callback dispatcher
    [Fact]
    public async Task DispatchAsync_WhenMiddlewareThrowsException_ShouldBubbleException()
    {
        // arrange
        var faultyMiddleware = new FaultyCallbackMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new CallbackPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyCallbackMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(FaultyCallbackMiddleware)] = faultyMiddleware
        });

        var dispatcher = new CallbackDispatcher(serviceProvider, pipelineBuilder);
        var query = new CallbackQuery
        {
            Id = "query_123",
            Data = "menu:main",
            From = new User { Id = 456, FirstName = "User" }
        };

        // act
        var act = () => dispatcher.DispatchAsync(query, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulated pipeline failure");
    }
}