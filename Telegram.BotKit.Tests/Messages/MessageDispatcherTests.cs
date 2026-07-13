using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Messages;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Messages;

public sealed class MessageDispatcherTests
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

    private sealed class DummyMessageMiddleware : IMessageMiddleware
    {
        public bool Called { get; private set; }

        public async Task InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Called = true;
            await next();
        }
    }

    private sealed class FaultyMessageMiddleware : IMessageMiddleware
    {
        public Task InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated pipeline failure");
        }
    }

    // verifies that a valid standard message successfully resolves and executes the registered middlewares in the pipeline
    [Fact]
    public async Task DispatchAsync_WhenMessageIsValid_ShouldBuildPipelineAndExecute()
    {
        // arrange
        var dummyMiddleware = new DummyMessageMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new MessagePipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyMessageMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(DummyMessageMiddleware)] = dummyMiddleware
        });

        var dispatcher = new MessageDispatcher(serviceProvider, pipelineBuilder);
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

    // verifies that an exception thrown inside a middleware bubbles up directly through the message dispatcher
    [Fact]
    public async Task DispatchAsync_WhenMiddlewareThrowsException_ShouldBubbleException()
    {
        // arrange
        var faultyMiddleware = new FaultyMessageMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new MessagePipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyMessageMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(FaultyMessageMiddleware)] = faultyMiddleware
        });

        var dispatcher = new MessageDispatcher(serviceProvider, pipelineBuilder);
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

    // verifies that passing a null message to the dispatcher throws an argument null exception
    [Fact]
    public async Task DispatchAsync_WhenMessageIsNull_ShouldThrowArgumentNullException()
    {
        // arrange
        var services = new ServiceCollection();
        var pipelineBuilder = new MessagePipelineBuilder(services);
        var serviceProvider = CreateMockServiceProvider([]);
        var dispatcher = new MessageDispatcher(serviceProvider, pipelineBuilder);

        // act
        var act = () => dispatcher.DispatchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}