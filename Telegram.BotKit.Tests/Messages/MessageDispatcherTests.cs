using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Messages;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Messages;

public sealed class MessageDispatcherTests
{
    private sealed class DummyMessageMiddleware : IMessageMiddleware
    {
        public bool Called { get; private set; }
        public CancellationToken CapturedToken { get; private set; }

        public ValueTask InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Called = true;
            CapturedToken = cancellationToken;
            return next();
        }
    }

    private sealed class FaultyMessageMiddleware : IMessageMiddleware
    {
        public ValueTask InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated pipeline failure");
    }

    // verifies that a valid standard message executes registered middlewares through the real async scope
    [Fact]
    public async Task DispatchAsync_WhenMessageIsValid_ShouldExecuteMiddlewares()
    {
        // arrange
        var dummyMiddleware = new DummyMessageMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(dummyMiddleware);

        var pipelineBuilder = new MessagePipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyMessageMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new MessageDispatcher(provider, compiledPipeline);

        var message = new Message
        {
            Id = 123,
            Chat = new Chat { Id = 1 },
            Text = "Hello Bot"
        };

        // act
        await dispatcher.DispatchAsync(message, TestContext.Current.CancellationToken);

        // assert
        dummyMiddleware.Called.Should().BeTrue();
    }

    // verifies that exceptions thrown inside middleware propagate through message dispatcher
    [Fact]
    public async Task DispatchAsync_WhenMiddlewareThrows_ShouldPropagateException()
    {
        // arrange
        var faultyMiddleware = new FaultyMessageMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(faultyMiddleware);

        var pipelineBuilder = new MessagePipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyMessageMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new MessageDispatcher(provider, compiledPipeline);

        var message = new Message
        {
            Id = 123,
            Chat = new Chat { Id = 1 },
            Text = "Hello Bot"
        };

        // act
        var act = () => dispatcher.DispatchAsync(message, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulated pipeline failure");
    }

    // verifies that passing null message throws argument null exception
    [Fact]
    public async Task DispatchAsync_WhenMessageIsNull_ShouldThrowArgumentNullException()
    {
        // arrange
        var services = new ServiceCollection();
        var pipelineBuilder = new MessagePipelineBuilder(services);
        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new MessageDispatcher(provider, compiledPipeline);

        // act
        var act = () => dispatcher.DispatchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // verifies that cancellation token flows from dispatcher to message middleware
    [Fact]
    public async Task DispatchAsync_WhenCancellationTokenProvided_ShouldPassTokenToMiddleware()
    {
        // arrange
        var dummyMiddleware = new DummyMessageMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(dummyMiddleware);

        var pipelineBuilder = new MessagePipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyMessageMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new MessageDispatcher(provider, compiledPipeline);

        var message = new Message
        {
            Id = 123,
            Chat = new Chat { Id = 1 },
            Text = "Hello Bot"
        };

        using var cts = new CancellationTokenSource();

        // act
        await dispatcher.DispatchAsync(message, cts.Token);

        // assert
        dummyMiddleware.CapturedToken.Should().Be(cts.Token);
    }
}
