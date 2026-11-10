using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.Shared.Pipeline;
using Telegram.BotKit.WidgetQueries;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Models;
using Telegram.BotKit.WidgetQueries.Routing;

namespace Telegram.BotKit.Tests.WidgetQueries;

public sealed class WidgetQueryDispatcherTests
{
    private sealed class DummyWidgetQueryMiddleware : IWidgetQueryMiddleware
    {
        public bool Called { get; private set; }
        public WidgetQueryContext? CapturedContext { get; private set; }
        public CancellationToken CapturedToken { get; private set; }

        public ValueTask InvokeAsync(WidgetQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Called = true;
            CapturedContext = context;
            CapturedToken = cancellationToken;
            return next();
        }
    }

    private sealed class FaultyWidgetQueryMiddleware : IWidgetQueryMiddleware
    {
        public ValueTask InvokeAsync(WidgetQueryContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated pipeline failure");
    }

    private sealed class ScopedMarker;

    private sealed class ScopeCapturingMiddleware(ScopedMarker marker) : IWidgetQueryMiddleware
    {
        public static List<ScopedMarker> Seen { get; } = [];

        public ValueTask InvokeAsync(WidgetQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            lock (Seen)
            {
                Seen.Add(marker);
            }

            return next();
        }
    }

    private static (ServiceProvider Provider, WidgetQueryDispatcher Dispatcher) CreateDispatcher<TMiddleware>(TMiddleware middleware)
        where TMiddleware : class, IWidgetQueryMiddleware
    {
        var services = new ServiceCollection();
        services.AddSingleton(middleware);

        var pipelineBuilder = new WidgetQueryPipelineBuilder(services, new WidgetQueryHandlerRegistry());
        pipelineBuilder.UseMiddleware<TMiddleware>();

        var provider = services.BuildServiceProvider();
        return (provider, new WidgetQueryDispatcher(provider, pipelineBuilder.Build()));
    }

    // both kinds of query go through the same dispatcher
    private static Task Dispatch(WidgetQueryDispatcher dispatcher, bool copy, string data, CancellationToken cancellationToken) =>
        copy
            ? dispatcher.DispatchAsync(WidgetQueryTestData.Copy(data), cancellationToken)
            : dispatcher.DispatchAsync(WidgetQueryTestData.Original(data), cancellationToken);

    // verifies that a valid query of either kind executes the middleware with a context that tells the kind
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_WhenQueryIsValid_ShouldExecuteMiddlewareWithMatchingContext(bool copy)
    {
        // arrange
        var middleware = new DummyWidgetQueryMiddleware();
        var (provider, dispatcher) = CreateDispatcher(middleware);
        await using var providerScope = provider;

        // act
        await Dispatch(dispatcher, copy, "poll:vote?o=2", TestContext.Current.CancellationToken);

        // assert
        middleware.Called.Should().BeTrue();
        middleware.CapturedContext.Should().NotBeNull();
        middleware.CapturedContext!.IsCopy.Should().Be(copy);
        middleware.CapturedContext.Key.Should().Be("poll:vote");
    }

    // verifies that exceptions thrown inside middleware propagate through the dispatcher
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_WhenMiddlewareThrows_ShouldPropagateException(bool copy)
    {
        // arrange
        var (provider, dispatcher) = CreateDispatcher(new FaultyWidgetQueryMiddleware());
        await using var providerScope = provider;

        // act
        var act = () => Dispatch(dispatcher, copy, "poll:vote", TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulated pipeline failure");
    }

    // verifies that passing a null query throws argument null exception, for each kind of query
    [Fact]
    public async Task DispatchAsync_WhenOriginalQueryIsNull_ShouldThrowArgumentNullException()
    {
        var (provider, dispatcher) = CreateDispatcher(new DummyWidgetQueryMiddleware());
        await using var providerScope = provider;

        var act = () => dispatcher.DispatchAsync((WidgetQuery)null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DispatchAsync_WhenSharedQueryIsNull_ShouldThrowArgumentNullException()
    {
        var (provider, dispatcher) = CreateDispatcher(new DummyWidgetQueryMiddleware());
        await using var providerScope = provider;

        var act = () => dispatcher.DispatchAsync((SharedWidgetQuery)null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // verifies that cancellation token flows from dispatcher to widget query middleware
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchAsync_WhenCancellationTokenProvided_ShouldPassTokenToMiddleware(bool copy)
    {
        // arrange
        var middleware = new DummyWidgetQueryMiddleware();
        var (provider, dispatcher) = CreateDispatcher(middleware);
        await using var providerScope = provider;

        using var cts = new CancellationTokenSource();

        // act
        await Dispatch(dispatcher, copy, "poll:vote", cts.Token);

        // assert
        middleware.CapturedToken.Should().Be(cts.Token);
    }

    // verifies that every query is processed in its own dependency scope
    [Fact]
    public async Task DispatchAsync_WhenCalledTwice_ShouldUseSeparateDependencyScopes()
    {
        // arrange
        ScopeCapturingMiddleware.Seen.Clear();

        var services = new ServiceCollection();
        services.AddScoped<ScopedMarker>();

        var pipelineBuilder = new WidgetQueryPipelineBuilder(services, new WidgetQueryHandlerRegistry());
        pipelineBuilder.UseMiddleware<ScopeCapturingMiddleware>();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new WidgetQueryDispatcher(provider, pipelineBuilder.Build());

        // act
        await dispatcher.DispatchAsync(WidgetQueryTestData.Original("test"), TestContext.Current.CancellationToken);
        await dispatcher.DispatchAsync(WidgetQueryTestData.Copy("test"), TestContext.Current.CancellationToken);

        // assert
        ScopeCapturingMiddleware.Seen.Should().HaveCount(2);
        ScopeCapturingMiddleware.Seen[0].Should().NotBeSameAs(ScopeCapturingMiddleware.Seen[1]);
    }
}
