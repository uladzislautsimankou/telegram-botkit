using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackExecutionMiddlewareTests
{
    private sealed class DummyHandler;

    // verifies that execution middleware executes the matched callback invoker and does not call next
    [Fact]
    public async Task InvokeAsync_WhenMatchedRegistrationIsNotNull_ShouldExecuteInvokerAndNotCallNext()
    {
        // arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CallbackExecutionMiddleware(serviceProvider);
        var handler = new DummyHandler();

        var invokerCalled = false;
        object? capturedInstance = null;
        CallbackContext? capturedContext = null;
        var capturedToken = CancellationToken.None;

        CallbackInvokerDelegate invoker = (instance, ctx, _, ct) =>
        {
            invokerCalled = true;
            capturedInstance = instance;
            capturedContext = ctx;
            capturedToken = ct;
            return Task.CompletedTask;
        };

        var registration = new CallbackRegistration(typeof(DummyHandler), typeof(object), _ => handler, invoker);
        var query = new CallbackQuery { Id = "1", Data = "test" };
        var context = new CallbackContext(query)
        {
            MatchedRegistration = registration,
            MatchedCallbackMetadata = new CallbackMetadata(typeof(DummyHandler), () => handler)
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
        var middleware = new CallbackExecutionMiddleware(serviceProvider);
        var query = new CallbackQuery { Id = "1", Data = "test" };
        var context = new CallbackContext(query)
        {
            MatchedRegistration = null,
            MatchedCallbackMetadata = null
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
        var middleware = new CallbackExecutionMiddleware(serviceProvider);

        var invokerCalled = false;
        CallbackInvokerDelegate invoker = (_, _, _, _) =>
        {
            invokerCalled = true;
            return Task.CompletedTask;
        };

        var registration = new CallbackRegistration(typeof(DummyHandler), typeof(object), _ => new DummyHandler(), invoker);
        var query = new CallbackQuery { Id = "1", Data = "test" };
        var context = new CallbackContext(query)
        {
            MatchedRegistration = registration,
            MatchedCallbackMetadata = null
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
        var middleware = new CallbackExecutionMiddleware(Substitute.For<IServiceProvider>());
        var handler = new DummyHandler();
        var factoryCalls = 0;

        var registration = new CallbackRegistration(
            typeof(DummyHandler),
            typeof(object),
            _ => handler,
            static (_, _, _, _) => Task.CompletedTask);

        var context = new CallbackContext(new CallbackQuery { Id = "1", Data = "test" })
        {
            MatchedRegistration = registration,
            MatchedCallbackMetadata = new CallbackMetadata(typeof(DummyHandler), () => { factoryCalls++; return handler; })
        };

        factoryCalls.Should().Be(0);

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        factoryCalls.Should().Be(1);
    }
}
