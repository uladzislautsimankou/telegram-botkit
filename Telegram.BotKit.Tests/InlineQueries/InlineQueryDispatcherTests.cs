using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class InlineQueryDispatcherTests
{
    private readonly ITelegramBotClient _botClient = Substitute.For<ITelegramBotClient>();
    private readonly ILogger<InlineQueryDispatcher> _logger = Substitute.For<ILogger<InlineQueryDispatcher>>();

    public InlineQueryDispatcherTests()
    {
        _logger.BeginScope(Arg.Any<object>()).Returns(Substitute.For<IDisposable>());
        _botClient.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
    }

    private sealed class DummyInlineMiddleware : IInlineQueryMiddleware
    {
        public ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            context.AddResults([Substitute.For<InlineQueryResult>()]);
            context.MergeConfiguration(
                isPersonal: true,
                cacheTime: 600,
                nextOffset: "page_2",
                button: null);

            return next();
        }
    }

    private sealed class FaultyInlineMiddleware : IInlineQueryMiddleware
    {
        public ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated pipeline failure");
    }

    // verifies that valid inline query executes pipeline and calls answer inline query with expected parameters
    [Fact]
    public async Task DispatchAsync_WhenQueryIsValid_ShouldExecutePipelineAndAnswerInlineQuery()
    {
        // arrange
        var dummyMiddleware = new DummyInlineMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(dummyMiddleware);

        var pipelineBuilder = new InlineQueryPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyInlineMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new InlineQueryDispatcher(_botClient, provider, compiledPipeline, _logger);

        var query = new InlineQuery
        {
            Id = "query_123",
            Query = "search",
            From = new User { Id = 456, FirstName = "User" }
        };

        // act
        await dispatcher.DispatchAsync(query, TestContext.Current.CancellationToken);

        // assert
        await _botClient.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(req => IsExpectedAnswerRequest(req)),
            Arg.Any<CancellationToken>());
    }

    // verifies that pipeline failure logs error but still calls answer inline query to release client spinner
    [Fact]
    public async Task DispatchAsync_WhenPipelineThrows_ShouldLogErrorAndAnswerInlineQuery()
    {
        // arrange
        var faultyMiddleware = new FaultyInlineMiddleware();
        var services = new ServiceCollection();
        services.AddSingleton(faultyMiddleware);

        var pipelineBuilder = new InlineQueryPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyInlineMiddleware>();

        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new InlineQueryDispatcher(_botClient, provider, compiledPipeline, _logger);

        var query = new InlineQuery
        {
            Id = "query_123",
            Query = "search",
            From = new User { Id = 456, FirstName = "User" }
        };

        // act
        await dispatcher.DispatchAsync(query, TestContext.Current.CancellationToken);

        // assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex is InvalidOperationException && ex.Message == "Simulated pipeline failure"),
            Arg.Any<Func<object, Exception?, string>>());

        await _botClient.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(req => IsFallbackAnswerRequest(req)),
            Arg.Any<CancellationToken>());
    }

    // verifies that null query returns early without calling pipeline or api
    [Fact]
    public async Task DispatchAsync_WhenQueryIsNull_ShouldReturnEarlyAndNotExecutePipelineOrBotCall()
    {
        // arrange
        var services = new ServiceCollection();
        var pipelineBuilder = new InlineQueryPipelineBuilder(services);
        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new InlineQueryDispatcher(_botClient, provider, compiledPipeline, _logger);

        // act
        await dispatcher.DispatchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await _botClient.DidNotReceiveWithAnyArgs().SendRequest(
            Arg.Any<IRequest<bool>>(),
            Arg.Any<CancellationToken>());
    }

    // verifies that answer inline query network exceptions are caught and logged without rethrowing
    [Fact]
    public async Task DispatchAsync_WhenAnswerInlineQueryThrows_ShouldCatchAndLogErrorWithoutRethrowing()
    {
        // arrange
        _botClient.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new HttpRequestException("Network failure")));

        var services = new ServiceCollection();
        var pipelineBuilder = new InlineQueryPipelineBuilder(services);
        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new InlineQueryDispatcher(_botClient, provider, compiledPipeline, _logger);

        var query = new InlineQuery
        {
            Id = "query_123",
            Query = "search",
            From = new User { Id = 456, FirstName = "User" }
        };

        // act
        var act = () => dispatcher.DispatchAsync(query, TestContext.Current.CancellationToken);

        // assert
        await act.Should().NotThrowAsync();

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to send inline query answer")),
            Arg.Is<Exception>(ex => ex is HttpRequestException),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // verifies that cancellation token is passed down to bot client call
    [Fact]
    public async Task DispatchAsync_WhenCancellationTokenProvided_ShouldPassTokenToBotClient()
    {
        // arrange
        var services = new ServiceCollection();
        var pipelineBuilder = new InlineQueryPipelineBuilder(services);
        var compiledPipeline = pipelineBuilder.Build(static (middleware, context, next, ct) =>
            middleware.InvokeAsync(context, next, ct));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new InlineQueryDispatcher(_botClient, provider, compiledPipeline, _logger);

        var query = new InlineQuery
        {
            Id = "query_123",
            Query = "search",
            From = new User { Id = 456, FirstName = "User" }
        };

        using var cts = new CancellationTokenSource();

        // act
        await dispatcher.DispatchAsync(query, cts.Token);

        // assert
        await _botClient.Received(1).SendRequest(
            Arg.Any<IRequest<bool>>(),
            cts.Token);
    }

    private static bool IsExpectedAnswerRequest(IRequest<bool> req) =>
        req is AnswerInlineQueryRequest request
        && request.InlineQueryId == "query_123"
        && request.Results.Count() == 1
        && request.IsPersonal == true
        && request.CacheTime == 600
        && request.NextOffset == "page_2";

    private static bool IsFallbackAnswerRequest(IRequest<bool> req) =>
        req is AnswerInlineQueryRequest request
        && request.InlineQueryId == "query_123"
        && !request.Results.Any()
        && request.IsPersonal == false
        && request.CacheTime == null
        && request.NextOffset == null;
}