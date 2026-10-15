using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.InlineQueries;
using Telegram.BotKit.InlineQueries.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.InlineQueries;

public sealed class InlineQueryDispatcherTests
{
    // verifies that valid query creates dedicated async scope and invokes compiled pipeline
    [Fact]
    public async Task DispatchAsync_WhenQueryIsValid_ShouldCreateAsyncScopeAndExecutePipeline()
    {
        // arrange
        var services = new ServiceCollection();
        await using var rootProvider = services.BuildServiceProvider();

        var wasExecuted = false;
        IServiceProvider? capturedScopedProvider = null;
        InlineQueryContext? capturedContext = null;

        PipelineDelegate<InlineQueryContext> compiledPipeline = (ctx, sp, ct) =>
        {
            wasExecuted = true;
            capturedContext = ctx;
            capturedScopedProvider = sp;
            return ValueTask.CompletedTask;
        };

        var dispatcher = new InlineQueryDispatcher(rootProvider, compiledPipeline);

        var query = new InlineQuery
        {
            Id = "query_123",
            Query = "search",
            From = new User { Id = 456, FirstName = "User" }
        };

        // act
        await dispatcher.DispatchAsync(query, TestContext.Current.CancellationToken);

        // assert
        wasExecuted.Should().BeTrue();
        capturedContext.Should().NotBeNull();
        capturedContext!.Query.Id.Should().Be("query_123");
        capturedScopedProvider.Should().NotBeNull();
        capturedScopedProvider.Should().NotBeSameAs(rootProvider);
    }

    // verifies that null query throws argument null exception
    [Fact]
    public async Task DispatchAsync_WhenQueryIsNull_ShouldThrowArgumentNullException()
    {
        // arrange
        var services = new ServiceCollection();
        await using var provider = services.BuildServiceProvider();

        PipelineDelegate<InlineQueryContext> compiledPipeline = (ctx, sp, ct) => ValueTask.CompletedTask;
        var dispatcher = new InlineQueryDispatcher(provider, compiledPipeline);

        // act
        var act = () => dispatcher.DispatchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("query");
    }

    // verifies that cancellation token is forwarded to the compiled pipeline delegate
    [Fact]
    public async Task DispatchAsync_WhenCancellationTokenProvided_ShouldPassTokenToPipeline()
    {
        // arrange
        var services = new ServiceCollection();
        await using var provider = services.BuildServiceProvider();

        var capturedToken = CancellationToken.None;
        PipelineDelegate<InlineQueryContext> compiledPipeline = (ctx, sp, ct) =>
        {
            capturedToken = ct;
            return ValueTask.CompletedTask;
        };

        var dispatcher = new InlineQueryDispatcher(provider, compiledPipeline);

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
        capturedToken.Should().Be(cts.Token);
    }
}
