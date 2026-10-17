using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackDispatcherTests
{
    private sealed class DummyCallbackMiddleware : ICallbackMiddleware
    {
        public bool Called { get; private set; }
        public CancellationToken CapturedToken { get; private set; }

        public ValueTask InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Called = true;
            CapturedToken = cancellationToken;
            return next();
        }
    }

    private sealed class FaultyCallbackMiddleware : ICallbackMiddleware
    {
        public ValueTask InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated pipeline failure");
    }

    // verifies that a valid callback query executes registered middlewares through the real async scope
    [Fact]
    public async Task DispatchAsync_WhenQueryIsValid_ShouldExecuteMiddlewares()
    {
        // arrange
        var dummyMiddleware = new DummyCallbackMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(dummyMiddleware);

        var pipelineBuilder = new CallbackPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyCallbackMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new CallbackDispatcher(provider, compiledPipeline);

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

    // verifies that exceptions thrown inside middleware propagate through callback dispatcher
    [Fact]
    public async Task DispatchAsync_WhenMiddlewareThrows_ShouldPropagateException()
    {
        // arrange
        var faultyMiddleware = new FaultyCallbackMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(faultyMiddleware);

        var pipelineBuilder = new CallbackPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyCallbackMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new CallbackDispatcher(provider, compiledPipeline);

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

    // verifies that passing null query throws argument null exception
    [Fact]
    public async Task DispatchAsync_WhenQueryIsNull_ShouldThrowArgumentNullException()
    {
        // arrange
        var services = new ServiceCollection();
        var pipelineBuilder = new CallbackPipelineBuilder(services);
        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new CallbackDispatcher(provider, compiledPipeline);

        // act
        var act = () => dispatcher.DispatchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // verifies that cancellation token flows from dispatcher to callback middleware
    [Fact]
    public async Task DispatchAsync_WhenCancellationTokenProvided_ShouldPassTokenToMiddleware()
    {
        // arrange
        var dummyMiddleware = new DummyCallbackMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(dummyMiddleware);

        var pipelineBuilder = new CallbackPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyCallbackMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new CallbackDispatcher(provider, compiledPipeline);

        var query = new CallbackQuery
        {
            Id = "query_123",
            Data = "menu:main",
            From = new User { Id = 456, FirstName = "User" }
        };

        using var cts = new CancellationTokenSource();

        // act
        await dispatcher.DispatchAsync(query, cts.Token);

        // assert
        dummyMiddleware.CapturedToken.Should().Be(cts.Token);
    }
}
