using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Commands;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandDispatcherTests
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

    private sealed class DummyCommandMiddleware : ICommandMiddleware
    {
        public bool Called { get; private set; }

        public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Called = true;
            await next();
        }
    }

    private sealed class FaultyCommandMiddleware : ICommandMiddleware
    {
        public Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated pipeline failure");
        }
    }

    // verifies that a valid command message successfully resolves and executes the registered middlewares in the pipeline
    [Fact]
    public async Task DispatchAsync_WhenMessageIsValid_ShouldBuildPipelineAndExecute()
    {
        // arrange
        var dummyMiddleware = new DummyCommandMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new CommandPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyCommandMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(DummyCommandMiddleware)] = dummyMiddleware
        });

        var dispatcher = new CommandDispatcher(serviceProvider, pipelineBuilder);
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

    // verifies that an exception thrown inside a middleware bubbles up directly through the command dispatcher
    [Fact]
    public async Task DispatchAsync_WhenMiddlewareThrowsException_ShouldBubbleException()
    {
        // arrange
        var faultyMiddleware = new FaultyCommandMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new CommandPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyCommandMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(FaultyCommandMiddleware)] = faultyMiddleware
        });

        var dispatcher = new CommandDispatcher(serviceProvider, pipelineBuilder);
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
}