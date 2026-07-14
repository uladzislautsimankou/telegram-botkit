using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.ChosenInlineResults;

public sealed class ChosenInlineResultDispatcherTests
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

    private sealed class DummyChosenInlineResultMiddleware : IChosenInlineResultMiddleware
    {
        public bool Called { get; private set; }

        public async Task InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Called = true;
            await next();
        }
    }

    private sealed class FaultyChosenInlineResultMiddleware : IChosenInlineResultMiddleware
    {
        public Task InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated pipeline failure");
        }
    }

    // verifies that a valid chosen inline result successfully resolves and executes the registered middlewares in the pipeline
    [Fact]
    public async Task DispatchAsync_WhenChosenInlineResultIsValid_ShouldBuildPipelineAndExecute()
    {
        // arrange
        var dummyMiddleware = new DummyChosenInlineResultMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new ChosenInlineResultPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyChosenInlineResultMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(DummyChosenInlineResultMiddleware)] = dummyMiddleware
        });

        var dispatcher = new ChosenInlineResultDispatcher(serviceProvider, pipelineBuilder);
        var chosenResult = new ChosenInlineResult
        {
            ResultId = "res_123",
            From = new User { Id = 1, FirstName = "Test" },
            Query = "test query"
        };

        // act
        await dispatcher.DispatchAsync(chosenResult, TestContext.Current.CancellationToken);

        // assert
        dummyMiddleware.Called.Should().BeTrue();
    }

    // verifies that an exception thrown inside a middleware bubbles up directly through the dispatcher
    [Fact]
    public async Task DispatchAsync_WhenMiddlewareThrowsException_ShouldBubbleException()
    {
        // arrange
        var faultyMiddleware = new FaultyChosenInlineResultMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new ChosenInlineResultPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyChosenInlineResultMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(FaultyChosenInlineResultMiddleware)] = faultyMiddleware
        });

        var dispatcher = new ChosenInlineResultDispatcher(serviceProvider, pipelineBuilder);
        var chosenResult = new ChosenInlineResult
        {
            ResultId = "res_123",
            From = new User { Id = 1, FirstName = "Test" },
            Query = "test query"
        };

        // act
        var act = () => dispatcher.DispatchAsync(chosenResult, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulated pipeline failure");
    }

    // verifies that passing a null chosen inline result to the dispatcher throws an argument null exception
    [Fact]
    public async Task DispatchAsync_WhenChosenInlineResultIsNull_ShouldThrowArgumentNullException()
    {
        // arrange
        var services = new ServiceCollection();
        var pipelineBuilder = new ChosenInlineResultPipelineBuilder(services);
        var serviceProvider = CreateMockServiceProvider([]);
        var dispatcher = new ChosenInlineResultDispatcher(serviceProvider, pipelineBuilder);

        // act
        var act = () => dispatcher.DispatchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}