using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.ChosenInlineResults.Routing;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.ChosenInlineResults;

public sealed class ChosenInlineResultExecutionMiddlewareTests
{
    // verifies that the execution middleware invokes the matched handler when it is present in the context
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsNotNull_ShouldExecuteHandler()
    {
        // arrange
        var handler = Substitute.For<IChosenInlineResultHandler>();
        var middleware = new ChosenInlineResultExecutionMiddleware();

        var chosenResult = new ChosenInlineResult { ResultId = "res_123", From = new User { Id = 1, FirstName = "Test" } };
        var context = new ChosenInlineResultContext(chosenResult)
        {
            MatchedHandler = handler
        };

        NextDelegate next = () => Task.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        await handler.Received(1).HandleAsync(context, TestContext.Current.CancellationToken);
    }

    // verifies that the execution middleware cleanly completes without attempting execution when matched handler is null
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsNull_ShouldDoNothing()
    {
        // arrange
        var middleware = new ChosenInlineResultExecutionMiddleware();
        var chosenResult = new ChosenInlineResult { ResultId = "res_123", From = new User { Id = 1, FirstName = "Test" } };
        var context = new ChosenInlineResultContext(chosenResult)
        {
            MatchedHandler = null
        };

        NextDelegate next = () => Task.CompletedTask;

        // act
        var act = () => middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        await act.Should().NotThrowAsync();
    }
}