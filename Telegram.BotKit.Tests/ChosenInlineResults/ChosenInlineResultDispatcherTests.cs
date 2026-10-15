using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.ChosenInlineResults;

public sealed class ChosenInlineResultDispatcherTests
{
    private sealed class DummyChosenInlineResultMiddleware : IChosenInlineResultMiddleware
    {
        public bool Called { get; private set; }
        public CancellationToken CapturedToken { get; private set; }

        public ValueTask InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Called = true;
            CapturedToken = cancellationToken;
            return next();
        }
    }

    private sealed class FaultyChosenInlineResultMiddleware : IChosenInlineResultMiddleware
    {
        public ValueTask InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated pipeline failure");
    }

    // verifies that a valid chosen inline result executes registered middlewares through the real async scope
    [Fact]
    public async Task DispatchAsync_WhenChosenInlineResultIsValid_ShouldExecuteMiddlewares()
    {
        // arrange
        var dummyMiddleware = new DummyChosenInlineResultMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(dummyMiddleware);

        var pipelineBuilder = new ChosenInlineResultPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyChosenInlineResultMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new ChosenInlineResultDispatcher(provider, compiledPipeline);

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
    public async Task DispatchAsync_WhenMiddlewareThrows_ShouldPropagateException()
    {
        // arrange
        var faultyMiddleware = new FaultyChosenInlineResultMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(faultyMiddleware);

        var pipelineBuilder = new ChosenInlineResultPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyChosenInlineResultMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new ChosenInlineResultDispatcher(provider, compiledPipeline);

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
        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new ChosenInlineResultDispatcher(provider, compiledPipeline);

        // act
        var act = () => dispatcher.DispatchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // verifies that cancellation token flows from dispatcher to chosen inline result middleware
    [Fact]
    public async Task DispatchAsync_WhenCancellationTokenProvided_ShouldPassTokenToMiddleware()
    {
        // arrange
        var dummyMiddleware = new DummyChosenInlineResultMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(dummyMiddleware);

        var pipelineBuilder = new ChosenInlineResultPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyChosenInlineResultMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new ChosenInlineResultDispatcher(provider, compiledPipeline);

        var chosenResult = new ChosenInlineResult
        {
            ResultId = "res_123",
            From = new User { Id = 1, FirstName = "Test" },
            Query = "test query"
        };

        using var cts = new CancellationTokenSource();

        // act
        await dispatcher.DispatchAsync(chosenResult, cts.Token);

        // assert
        dummyMiddleware.CapturedToken.Should().Be(cts.Token);
    }
}
