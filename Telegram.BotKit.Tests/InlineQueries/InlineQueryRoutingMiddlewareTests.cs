using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.InlineQueries.Routing;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class InlineQueryRoutingMiddlewareTests
{
    private readonly ILogger<InlineQueryRoutingMiddleware> _logger = Substitute.For<ILogger<InlineQueryRoutingMiddleware>>();

    // IInlineQueryHandler has static abstract members and cannot be substituted, so tests use real handler types
    // whose behaviour is configured per test through the Scenario registered in DI
    private sealed class Scenario
    {
        private readonly ConcurrentDictionary<Type, Func<Task<InlineQueryBatch>>> _behaviors = new();
        private int _constructed;

        public ConcurrentQueue<Guid> ObservedScopes { get; } = new();
        public int Constructed => _constructed;

        public void OnConstructed() => Interlocked.Increment(ref _constructed);

        public void Returns<THandler>(InlineQueryBatch batch) =>
            _behaviors[typeof(THandler)] = () => Task.FromResult(batch);

        public void Throws<THandler>(Exception exception) =>
            _behaviors[typeof(THandler)] = () => Task.FromException<InlineQueryBatch>(exception);

        public Task<InlineQueryBatch> RunAsync<THandler>(ScopedMarker marker)
        {
            ObservedScopes.Enqueue(marker.Id);

            return _behaviors.TryGetValue(typeof(THandler), out var behavior)
                ? behavior()
                : Task.FromResult(new InlineQueryBatch());
        }
    }

    private sealed class ScopedMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class HandlerA(Scenario scenario, ScopedMarker marker) : IInlineQueryHandler
    {
        public static bool CanHandle(InlineQuery query) => true;

        public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken = default) =>
            scenario.RunAsync<HandlerA>(marker);
    }

    private sealed class HandlerC(Scenario scenario, ScopedMarker marker) : IInlineQueryHandler
    {
        public static int Order => -1;

        public static bool CanHandle(InlineQuery query) => true;

        public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken = default) =>
            scenario.RunAsync<HandlerC>(marker);
    }

    private sealed class HandlerB(Scenario scenario, ScopedMarker marker) : IInlineQueryHandler
    {
        public static bool CanHandle(InlineQuery query) => true;

        public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken = default) =>
            scenario.RunAsync<HandlerB>(marker);
    }

    private sealed class NonMatchingHandler : IInlineQueryHandler
    {
        public NonMatchingHandler(Scenario scenario) => scenario.OnConstructed();

        public static bool CanHandle(InlineQuery query) => false;

        public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("A handler that cannot handle the query must never be invoked.");
    }

    // mirrors production wiring: the middleware gets the request scope provider and the root scope factory
    private sealed class Harness : IAsyncDisposable
    {
        private readonly ServiceProvider _root;
        private readonly AsyncServiceScope _requestScope;

        public Harness(InlineQueryExecutionMode mode, ILogger<InlineQueryRoutingMiddleware> logger, Type[] handlerTypes)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Scenario);
            services.AddScoped<ScopedMarker>();

            var registry = new InlineQueryHandlerRegistry();
            foreach (var handlerType in handlerTypes)
            {
                InlineQueryRegistrationHelper.RegisterHandlerByType(services, registry, handlerType);
            }

            registry.Build();

            _root = services.BuildServiceProvider();
            _requestScope = _root.CreateAsyncScope();

            Middleware = new InlineQueryRoutingMiddleware(
                registry,
                new InlineQueryPipelineSettings(mode),
                _requestScope.ServiceProvider,
                _root.GetRequiredService<IServiceScopeFactory>(),
                logger);
        }

        public Scenario Scenario { get; } = new();

        public InlineQueryRoutingMiddleware Middleware { get; }

        public Guid RequestScopeId => _requestScope.ServiceProvider.GetRequiredService<ScopedMarker>().Id;

        public async ValueTask DisposeAsync()
        {
            await _requestScope.DisposeAsync();
            await _root.DisposeAsync();
        }
    }

    private Harness CreateHarness(InlineQueryExecutionMode mode, params Type[] handlerTypes) =>
        new(mode, _logger, handlerTypes);

    private static async Task<bool> InvokeAsync(Harness harness, InlineQueryContext context, CancellationToken? cancellationToken = null)
    {
        var nextCalled = false;
        await harness.Middleware.InvokeAsync(
            context,
            () => { nextCalled = true; return ValueTask.CompletedTask; },
            cancellationToken ?? TestContext.Current.CancellationToken);

        return nextCalled;
    }

    // verifies that when no handler matches, handlers are never created and the pipeline continues unhandled
    [Fact]
    public async Task InvokeAsync_WhenNoHandlersCanHandle_ShouldCallNextWithoutCreatingHandlers()
    {
        // arrange
        await using var harness = CreateHarness(InlineQueryExecutionMode.Parallel, typeof(NonMatchingHandler));
        var context = CreateInlineContext("search query");

        // act
        var nextCalled = await InvokeAsync(harness, context);

        // assert
        nextCalled.Should().BeTrue();
        context.IsHandled.Should().BeFalse();
        context.Results.Should().BeEmpty();
        harness.Scenario.Constructed.Should().Be(0);
    }

    // verifies that only matching handlers are created and executed
    [Fact]
    public async Task InvokeAsync_WhenOnlySomeHandlersMatch_ShouldRunOnlyMatchingOnes()
    {
        // arrange
        var result = Substitute.For<InlineQueryResult>();
        await using var harness = CreateHarness(InlineQueryExecutionMode.Parallel, typeof(HandlerA), typeof(NonMatchingHandler));
        harness.Scenario.Returns<HandlerA>(new InlineQueryBatch([result]));
        var context = CreateInlineContext("search query");

        // act
        await InvokeAsync(harness, context);

        // assert
        context.IsHandled.Should().BeTrue();
        context.Results.Should().ContainSingle().Which.Should().BeSameAs(result);
        harness.Scenario.Constructed.Should().Be(0);
    }

    // verifies that a single matching handler populates the context and sets IsHandled
    [Fact]
    public async Task InvokeAsync_WhenSingleHandlerMatches_ShouldInvokeHandlerAndSetIsHandledTrue()
    {
        // arrange
        var result = Substitute.For<InlineQueryResult>();
        await using var harness = CreateHarness(InlineQueryExecutionMode.Parallel, typeof(HandlerA));
        harness.Scenario.Returns<HandlerA>(new InlineQueryBatch([result], isPersonal: true, cacheTime: 300, nextOffset: "offset_val"));
        var context = CreateInlineContext("search query");

        // act
        var nextCalled = await InvokeAsync(harness, context);

        // assert
        nextCalled.Should().BeTrue();
        context.IsHandled.Should().BeTrue();
        context.Results.Should().ContainSingle().Which.Should().BeSameAs(result);
        context.IsPersonal.Should().BeTrue();
        context.CacheTime.Should().Be(300);
        context.NextOffset.Should().Be("offset_val");
    }

    // verifies that multiple matching handlers are aggregated in registration order
    [Theory]
    [InlineData(InlineQueryExecutionMode.Parallel)]
    [InlineData(InlineQueryExecutionMode.Sequential)]
    public async Task InvokeAsync_WhenMultipleHandlersMatch_ShouldAggregateResultsInRegistrationOrder(InlineQueryExecutionMode mode)
    {
        // arrange
        var result1 = Substitute.For<InlineQueryResult>();
        var result2 = Substitute.For<InlineQueryResult>();
        await using var harness = CreateHarness(mode, typeof(HandlerA), typeof(HandlerB));
        harness.Scenario.Returns<HandlerA>(new InlineQueryBatch([result1], isPersonal: false, cacheTime: 600, nextOffset: "offset1"));
        harness.Scenario.Returns<HandlerB>(new InlineQueryBatch([result2], isPersonal: true, cacheTime: 300, nextOffset: "offset2"));
        var context = CreateInlineContext("search query");

        // act
        var nextCalled = await InvokeAsync(harness, context);

        // assert
        nextCalled.Should().BeTrue();
        context.IsHandled.Should().BeTrue();
        context.Results.Should().Equal(result1, result2);
        context.IsPersonal.Should().BeTrue();
        context.CacheTime.Should().Be(300);
        context.NextOffset.Should().Be("offset2");
    }

    // verifies that a failing handler is captured and logged without breaking the pipeline
    [Fact]
    public async Task InvokeAsync_WhenHandlerThrows_ShouldCaptureExceptionLogAndCallNext()
    {
        // arrange
        await using var harness = CreateHarness(InlineQueryExecutionMode.Parallel, typeof(HandlerA));
        harness.Scenario.Throws<HandlerA>(new InvalidOperationException("DB error"));
        var context = CreateInlineContext("search query");

        // act
        var nextCalled = await InvokeAsync(harness, context);

        // assert
        nextCalled.Should().BeTrue();
        context.IsHandled.Should().BeTrue();
        context.Exceptions.Should().ContainSingle()
            .Which.Should().BeOfType<HandlerFailedException>()
            .Which.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("DB error");

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex is InvalidOperationException && ex.Message == "DB error"),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that one failing handler does not prevent the others from contributing results
    [Theory]
    [InlineData(InlineQueryExecutionMode.Parallel)]
    [InlineData(InlineQueryExecutionMode.Sequential)]
    public async Task InvokeAsync_WhenOneHandlerThrowsAmongMultiple_ShouldCaptureExceptionAndAggregateRemainingResults(InlineQueryExecutionMode mode)
    {
        // arrange
        var successResult = Substitute.For<InlineQueryResult>();
        await using var harness = CreateHarness(mode, typeof(HandlerA), typeof(HandlerB));
        harness.Scenario.Throws<HandlerA>(new InvalidOperationException("API failure"));
        harness.Scenario.Returns<HandlerB>(new InlineQueryBatch([successResult]));
        var context = CreateInlineContext("search query");

        // act
        var nextCalled = await InvokeAsync(harness, context);

        // assert
        nextCalled.Should().BeTrue();
        context.IsHandled.Should().BeTrue();
        context.Results.Should().ContainSingle().Which.Should().BeSameAs(successResult);
        context.Exceptions.Should().ContainSingle().Which.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    // verifies that a handler's button is set on the context
    [Fact]
    public async Task InvokeAsync_WhenSingleHandlerProvidesButton_ShouldPopulateButtonInContext()
    {
        // arrange
        var button = new InlineQueryResultsButton("Test Button") { StartParameter = "start_param" };
        await using var harness = CreateHarness(InlineQueryExecutionMode.Parallel, typeof(HandlerA));
        harness.Scenario.Returns<HandlerA>(new InlineQueryBatch([Substitute.For<InlineQueryResult>()], button: button));
        var context = CreateInlineContext("search query");

        // act
        await InvokeAsync(harness, context);

        // assert
        context.Button.Should().BeSameAs(button);
    }

    // verifies that when several handlers provide a button the last one wins and the conflict is logged as a warning
    [Fact]
    public async Task InvokeAsync_WhenMultipleHandlersProvideButton_ShouldKeepLastButtonAndLogWarning()
    {
        // arrange
        var button1 = new InlineQueryResultsButton("Button 1") { StartParameter = "param1" };
        var button2 = new InlineQueryResultsButton("Button 2") { StartParameter = "param2" };
        await using var harness = CreateHarness(InlineQueryExecutionMode.Parallel, typeof(HandlerA), typeof(HandlerB));
        harness.Scenario.Returns<HandlerA>(new InlineQueryBatch([Substitute.For<InlineQueryResult>()], button: button1));
        harness.Scenario.Returns<HandlerB>(new InlineQueryBatch([Substitute.For<InlineQueryResult>()], button: button2));
        var context = CreateInlineContext("search query");

        // act
        await InvokeAsync(harness, context);

        // assert
        context.Button.Should().BeSameAs(button2);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("replaced the results button")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that concurrently running handlers get their own dependency scopes
    [Fact]
    public async Task InvokeAsync_WhenHandlersRunInParallel_ShouldResolveEachHandlerInItsOwnScope()
    {
        // arrange
        await using var harness = CreateHarness(InlineQueryExecutionMode.Parallel, typeof(HandlerA), typeof(HandlerB));
        var context = CreateInlineContext("search query");

        // act
        await InvokeAsync(harness, context);

        // assert
        harness.Scenario.ObservedScopes.Should().HaveCount(2);
        harness.Scenario.ObservedScopes.Distinct().Should().HaveCount(2);
        harness.Scenario.ObservedScopes.Should().NotContain(harness.RequestScopeId);
    }

    // verifies that sequential mode keeps all handlers inside the request scope
    [Fact]
    public async Task InvokeAsync_WhenModeIsSequential_ShouldResolveHandlersInRequestScope()
    {
        // arrange
        await using var harness = CreateHarness(InlineQueryExecutionMode.Sequential, typeof(HandlerA), typeof(HandlerB));
        var context = CreateInlineContext("search query");

        // act
        await InvokeAsync(harness, context);

        // assert
        harness.Scenario.ObservedScopes.Should().HaveCount(2)
            .And.OnlyContain(id => id == harness.RequestScopeId);
    }

    // verifies that a single matching handler runs in the request scope even in parallel mode
    [Fact]
    public async Task InvokeAsync_WhenSingleHandlerMatchesInParallelMode_ShouldResolveItInRequestScope()
    {
        // arrange
        await using var harness = CreateHarness(InlineQueryExecutionMode.Parallel, typeof(HandlerA));
        var context = CreateInlineContext("search query");

        // act
        await InvokeAsync(harness, context);

        // assert
        harness.Scenario.ObservedScopes.Should().ContainSingle().Which.Should().Be(harness.RequestScopeId);
    }

    // verifies that request cancellation propagates and is neither captured nor logged as a handler failure
    [Theory]
    [InlineData(InlineQueryExecutionMode.Parallel)]
    [InlineData(InlineQueryExecutionMode.Sequential)]
    public async Task InvokeAsync_WhenRequestIsCancelled_ShouldPropagateCancellationWithoutReportingFailure(InlineQueryExecutionMode mode)
    {
        // arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await using var harness = CreateHarness(mode, typeof(HandlerA), typeof(HandlerB));
        harness.Scenario.Throws<HandlerA>(new OperationCanceledException(cts.Token));
        harness.Scenario.Throws<HandlerB>(new OperationCanceledException(cts.Token));
        var context = CreateInlineContext("search query");

        // act
        var act = () => InvokeAsync(harness, context, cts.Token);

        // assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        context.Exceptions.Should().BeEmpty();

        _logger.DidNotReceive().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that an OperationCanceledException from a handler is a regular failure when the request itself is still alive
    [Fact]
    public async Task InvokeAsync_WhenHandlerThrowsCancellationButRequestIsAlive_ShouldCaptureAsFailure()
    {
        // arrange
        await using var harness = CreateHarness(InlineQueryExecutionMode.Parallel, typeof(HandlerA));
        harness.Scenario.Throws<HandlerA>(new OperationCanceledException("internal timeout"));
        var context = CreateInlineContext("search query");

        // act
        var nextCalled = await InvokeAsync(harness, context);

        // assert
        nextCalled.Should().BeTrue();
        context.Exceptions.Should().ContainSingle()
            .Which.Should().BeOfType<HandlerFailedException>()
            .Which.InnerException.Should().BeOfType<OperationCanceledException>();
    }

    private static InlineQueryContext CreateInlineContext(string queryText)
    {
        var query = new InlineQuery
        {
            Id = "1",
            Query = queryText,
            From = new User { Id = 123, FirstName = "User" }
        };

        return new InlineQueryContext(query);
    }

    // verifies that results are aggregated by handler Order, not by registration order
    [Theory]
    [InlineData(InlineQueryExecutionMode.Parallel)]
    [InlineData(InlineQueryExecutionMode.Sequential)]
    public async Task InvokeAsync_WhenHandlersDeclareOrder_ShouldAggregateResultsByOrder(InlineQueryExecutionMode mode)
    {
        // arrange
        var resultA = Substitute.For<InlineQueryResult>();
        var resultC = Substitute.For<InlineQueryResult>();

        // HandlerC is registered after HandlerA but declares a lower Order
        await using var harness = CreateHarness(mode, typeof(HandlerA), typeof(HandlerC));
        harness.Scenario.Returns<HandlerA>(new InlineQueryBatch([resultA]));
        harness.Scenario.Returns<HandlerC>(new InlineQueryBatch([resultC]));
        var context = CreateInlineContext("search query");

        // act
        await InvokeAsync(harness, context);

        // assert
        context.Results.Should().Equal(resultC, resultA);
    }
}
