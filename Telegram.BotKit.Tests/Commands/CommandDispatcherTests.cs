using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Commands;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandDispatcherTests
{
    private sealed class DummyCommandMiddleware : ICommandMiddleware
    {
        public bool Called { get; private set; }
        public CancellationToken CapturedToken { get; private set; }

        public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Called = true;
            CapturedToken = cancellationToken;
            return next();
        }
    }

    private sealed class FaultyCommandMiddleware : ICommandMiddleware
    {
        public ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated pipeline failure");
    }

    // verifies that a valid command message executes registered middlewares through the real async scope
    [Fact]
    public async Task DispatchAsync_WhenMessageIsValid_ShouldExecuteMiddlewares()
    {
        // arrange
        var dummyMiddleware = new DummyCommandMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(dummyMiddleware);

        var pipelineBuilder = new CommandPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyCommandMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new CommandDispatcher(provider, compiledPipeline);

        var message = new Message
        {
            Id = 123,
            Chat = new Chat { Id = 1 },
            Text = "/start"
        };

        // act
        await dispatcher.DispatchAsync(message, TestContext.Current.CancellationToken);

        // assert
        dummyMiddleware.Called.Should().BeTrue();
    }

    // verifies that exceptions thrown inside middleware propagate through dispatcher
    [Fact]
    public async Task DispatchAsync_WhenMiddlewareThrows_ShouldPropagateException()
    {
        // arrange
        var faultyMiddleware = new FaultyCommandMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(faultyMiddleware);

        var pipelineBuilder = new CommandPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyCommandMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new CommandDispatcher(provider, compiledPipeline);

        var message = new Message
        {
            Id = 123,
            Chat = new Chat { Id = 1 },
            Text = "/start"
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
        var pipelineBuilder = new CommandPipelineBuilder(services);
        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new CommandDispatcher(provider, compiledPipeline);

        // act
        var act = () => dispatcher.DispatchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // verifies that cancellation token flows from dispatcher to middleware
    [Fact]
    public async Task DispatchAsync_WhenCancellationTokenProvided_ShouldPassTokenToMiddleware()
    {
        // arrange
        var dummyMiddleware = new DummyCommandMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(dummyMiddleware);

        var pipelineBuilder = new CommandPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyCommandMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new CommandDispatcher(provider, compiledPipeline);

        var message = new Message
        {
            Id = 123,
            Chat = new Chat { Id = 1 },
            Text = "/start"
        };

        using var cts = new CancellationTokenSource();

        // act
        await dispatcher.DispatchAsync(message, cts.Token);

        // assert
        dummyMiddleware.CapturedToken.Should().Be(cts.Token);
    }
}
