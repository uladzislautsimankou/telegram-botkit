using FluentAssertions;
using NSubstitute;
using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries.Invocation;
using Telegram.BotKit.WidgetQueries.Models;
using Telegram.BotKit.WidgetQueries.Routing;

namespace Telegram.BotKit.Tests.WidgetQueries;

public sealed class WidgetQueryExecutionMiddlewareTests
{
    private sealed class DummyHandler;

    // verifies that execution middleware executes the matched widget query invoker and does not call next
    [Fact]
    public async Task InvokeAsync_WhenMatchedRegistrationIsNotNull_ShouldExecuteInvokerAndNotCallNext()
    {
        // arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new WidgetQueryExecutionMiddleware(serviceProvider);
        var handler = new DummyHandler();

        var invokerCalled = false;
        object? capturedInstance = null;
        WidgetQueryContext? capturedContext = null;
        var capturedToken = CancellationToken.None;

        WidgetQueryInvokerDelegate invoker = (instance, ctx, _, ct) =>
        {
            invokerCalled = true;
            capturedInstance = instance;
            capturedContext = ctx;
            capturedToken = ct;
            return Task.CompletedTask;
        };

        var registration = new WidgetQueryRegistration(typeof(DummyHandler), typeof(object), _ => handler, invoker);
        var context = WidgetQueryTestData.Context("test");
        context.MatchedRegistration = registration;
        context.MatchedWidgetQueryMetadata = new WidgetQueryMetadata(typeof(DummyHandler), () => handler);

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

    // verifies that the invoker also gets the context of a forwarded copy
    [Fact]
    public async Task InvokeAsync_WhenContextIsCopy_ShouldPassCopyContextToInvoker()
    {
        // arrange
        var middleware = new WidgetQueryExecutionMiddleware(Substitute.For<IServiceProvider>());
        var handler = new DummyHandler();
        WidgetQueryContext? capturedContext = null;

        var registration = new WidgetQueryRegistration(
            typeof(DummyHandler),
            typeof(object),
            _ => handler,
            (_, ctx, _, _) =>
            {
                capturedContext = ctx;
                return Task.CompletedTask;
            });

        var context = new WidgetQueryContext(WidgetQueryTestData.Copy("test"))
        {
            MatchedRegistration = registration,
            MatchedWidgetQueryMetadata = new WidgetQueryMetadata(typeof(DummyHandler), () => handler)
        };

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        capturedContext.Should().BeSameAs(context);
        capturedContext!.IsCopy.Should().BeTrue();
    }

    // verifies that execution middleware cleanly completes without calling next when matched registration is null
    [Fact]
    public async Task InvokeAsync_WhenMatchedRegistrationIsNull_ShouldCompleteWithoutCallingNext()
    {
        // arrange
        var middleware = new WidgetQueryExecutionMiddleware(Substitute.For<IServiceProvider>());
        var context = WidgetQueryTestData.Context("test");

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
        var middleware = new WidgetQueryExecutionMiddleware(Substitute.For<IServiceProvider>());

        var invokerCalled = false;
        WidgetQueryInvokerDelegate invoker = (_, _, _, _) =>
        {
            invokerCalled = true;
            return Task.CompletedTask;
        };

        var context = WidgetQueryTestData.Context("test");
        context.MatchedRegistration = new WidgetQueryRegistration(typeof(DummyHandler), typeof(object), _ => new DummyHandler(), invoker);

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
        var middleware = new WidgetQueryExecutionMiddleware(Substitute.For<IServiceProvider>());
        var handler = new DummyHandler();
        var factoryCalls = 0;

        var registration = new WidgetQueryRegistration(
            typeof(DummyHandler),
            typeof(object),
            _ => handler,
            static (_, _, _, _) => Task.CompletedTask);

        var context = WidgetQueryTestData.Context("test");
        context.MatchedRegistration = registration;
        context.MatchedWidgetQueryMetadata = new WidgetQueryMetadata(typeof(DummyHandler), () => { factoryCalls++; return handler; });

        factoryCalls.Should().Be(0);

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        factoryCalls.Should().Be(1);
    }
}
