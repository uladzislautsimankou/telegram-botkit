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
        // mock logger scopes to prevent NullReferenceException on using blocks
        _logger.BeginScope(Arg.Any<object>()).Returns(Substitute.For<IDisposable>());

        // mock the virtual SendRequest method to return a successful task for AnswerInlineQuery Request
        _botClient.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
    }

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

    private sealed class DummyInlineMiddleware : IInlineQueryMiddleware
    {
        public async Task InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            context.AddResults([Substitute.For<InlineQueryResult>()]);

            context.MergeConfiguration(
                isPersonal: true,
                cacheTime: 600,
                nextOffset: "page_2",
                button: null);

            await next();
        }
    }

    private sealed class FaultyInlineMiddleware : IInlineQueryMiddleware
    {
        public Task InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated pipeline failure");
        }
    }

    // verifies that a valid inline query builds the pipeline, executes middlewares, and correctly calls answer inline query with expected parameters
    [Fact]
    public async Task DispatchAsync_WhenQueryIsValid_ShouldBuildPipelineExecuteAndAnswerInlineQuery()
    {
        // arrange
        var dummyMiddleware = new DummyInlineMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new InlineQueryPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<DummyInlineMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(DummyInlineMiddleware)] = dummyMiddleware
        });

        var dispatcher = new InlineQueryDispatcher(_botClient, serviceProvider, pipelineBuilder, _logger);
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
            Arg.Is<IRequest<bool>>(req =>
                req is AnswerInlineQueryRequest &&
                ((AnswerInlineQueryRequest)req).InlineQueryId == "query_123" &&
                ((AnswerInlineQueryRequest)req).Results.Count() == 1 &&
                ((AnswerInlineQueryRequest)req).IsPersonal == true &&
                ((AnswerInlineQueryRequest)req).CacheTime == 600 &&
                ((AnswerInlineQueryRequest)req).NextOffset == "page_2"
            ),
            Arg.Any<CancellationToken>()
        );
    }

    // verifies that if the pipeline execution fails, the exception is caught and logged, but answer inline query is still called to release the telegram client spinner
    [Fact]
    public async Task DispatchAsync_WhenPipelineThrowsException_ShouldLogErrorAndStillCallAnswerInlineQuery()
    {
        // arrange
        var faultyMiddleware = new FaultyInlineMiddleware();
        var services = new ServiceCollection();
        var pipelineBuilder = new InlineQueryPipelineBuilder(services);
        pipelineBuilder.UseMiddleware<FaultyInlineMiddleware>();

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [typeof(FaultyInlineMiddleware)] = faultyMiddleware
        });

        var dispatcher = new InlineQueryDispatcher(_botClient, serviceProvider, pipelineBuilder, _logger);
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
            Arg.Any<Func<object, Exception?, string>>()
        );

        await _botClient.Received(1).SendRequest(
            Arg.Is<IRequest<bool>>(req =>
                req is AnswerInlineQueryRequest &&
                ((AnswerInlineQueryRequest)req).InlineQueryId == "query_123" &&
                !((AnswerInlineQueryRequest)req).Results.Any() &&
                ((AnswerInlineQueryRequest)req).IsPersonal == false &&
                ((AnswerInlineQueryRequest)req).CacheTime == null &&
                ((AnswerInlineQueryRequest)req).NextOffset == null
            ),
            Arg.Any<CancellationToken>()
        );
    }

    // verifies that if the incoming query is null, the dispatcher exits early without attempting pipeline execution or bot API calls
    [Fact]
    public async Task DispatchAsync_WhenQueryIsNull_ShouldReturnEarlyAndNotExecutePipelineOrBotCall()
    {
        // arrange
        var services = new ServiceCollection();
        var pipelineBuilder = new InlineQueryPipelineBuilder(services);
        var serviceProvider = CreateMockServiceProvider([]);
        var dispatcher = new InlineQueryDispatcher(_botClient, serviceProvider, pipelineBuilder, _logger);

        // act
        await dispatcher.DispatchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await _botClient.DidNotReceiveWithAnyArgs().SendRequest(
            Arg.Any<IRequest<bool>>(),
            Arg.Any<CancellationToken>()
        );
    }

    // verifies that if sending the answer fails the exception is caught and logged without crashing the dispatcher
    [Fact]
    public async Task DispatchAsync_WhenAnswerInlineQueryThrows_ShouldCatchAndLogErrorWithoutRethrowing()
    {
        // arrange
        _botClient.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new HttpRequestException("Network failure")));

        var services = new ServiceCollection();
        var pipelineBuilder = new InlineQueryPipelineBuilder(services);
        var serviceProvider = CreateMockServiceProvider([]);
        var dispatcher = new InlineQueryDispatcher(_botClient, serviceProvider, pipelineBuilder, _logger);
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
            Arg.Any<Func<object, Exception?, string>>()
        );
    }
}