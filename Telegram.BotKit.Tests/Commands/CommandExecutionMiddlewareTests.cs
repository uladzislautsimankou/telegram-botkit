using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandExecutionMiddlewareTests
{
    private sealed class DummyHandler;

    // verifies that execution middleware executes the matched command invoker and does not call next
    [Fact]
    public async Task InvokeAsync_WhenMatchedRegistrationIsNotNull_ShouldExecuteInvokerAndNotCallNext()
    {
        // arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandExecutionMiddleware(serviceProvider);
        var handler = new DummyHandler();

        var invokerCalled = false;
        object? capturedInstance = null;
        CommandContext? capturedContext = null;
        var capturedToken = CancellationToken.None;

        CommandInvokerDelegate invoker = (instance, ctx, _, ct) =>
        {
            invokerCalled = true;
            capturedInstance = instance;
            capturedContext = ctx;
            capturedToken = ct;
            return Task.CompletedTask;
        };

        var registration = new CommandRegistration(typeof(DummyHandler), typeof(object), _ => handler, invoker);
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new CommandContext(message)
        {
            MatchedRegistration = registration,
            MatchedCommandMetadata = new CommandMetadata(typeof(DummyHandler), () => handler)
        };

        var nextCalled = false;
        NextDelegate next = () =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        invokerCalled.Should().BeTrue();
        capturedInstance.Should().BeSameAs(handler);
        capturedContext.Should().BeSameAs(context);
        capturedToken.Should().Be(TestContext.Current.CancellationToken);
        nextCalled.Should().BeFalse();
    }

    // verifies that execution middleware cleanly completes without calling next when matched registration is null
    [Fact]
    public async Task InvokeAsync_WhenMatchedRegistrationIsNull_ShouldCompleteWithoutCallingNext()
    {
        // arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandExecutionMiddleware(serviceProvider);
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new CommandContext(message)
        {
            MatchedRegistration = null,
            MatchedCommandMetadata = null
        };

        var nextCalled = false;
        NextDelegate next = () =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        var result = middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);
        await result;

        // assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        nextCalled.Should().BeFalse();
    }

    // verifies that execution middleware completes safely when registration is present but metadata is missing
    [Fact]
    public async Task InvokeAsync_WhenMetadataIsNull_ShouldCompleteWithoutInvokingOrCallingNext()
    {
        // arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandExecutionMiddleware(serviceProvider);

        var invokerCalled = false;
        CommandInvokerDelegate invoker = (_, _, _, _) =>
        {
            invokerCalled = true;
            return Task.CompletedTask;
        };

        var registration = new CommandRegistration(typeof(DummyHandler), typeof(object), _ => new DummyHandler(), invoker);
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new CommandContext(message)
        {
            MatchedRegistration = registration,
            MatchedCommandMetadata = null
        };

        var nextCalled = false;
        NextDelegate next = () =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        var result = middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);
        await result;

        // assert
        result.IsCompletedSuccessfully.Should().BeTrue();
        invokerCalled.Should().BeFalse();
        nextCalled.Should().BeFalse();
    }

    // verifies that the handler is created when execution runs, and exactly once
    [Fact]
    public async Task InvokeAsync_WhenExecuting_ShouldCreateHandlerExactlyOnce()
    {
        // arrange
        var middleware = new CommandExecutionMiddleware(Substitute.For<IServiceProvider>());
        var handler = new DummyHandler();
        var factoryCalls = 0;

        var registration = new CommandRegistration(
            typeof(DummyHandler),
            typeof(object),
            _ => handler,
            static (_, _, _, _) => Task.CompletedTask);

        var context = new CommandContext(new Message { Text = "/test", Chat = new Chat { Id = 1 } })
        {
            MatchedRegistration = registration,
            MatchedCommandMetadata = new CommandMetadata(typeof(DummyHandler), () => { factoryCalls++; return handler; })
        };

        factoryCalls.Should().Be(0);

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        factoryCalls.Should().Be(1);
    }
}
