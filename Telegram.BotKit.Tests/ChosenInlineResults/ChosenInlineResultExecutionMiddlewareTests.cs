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
    // verifies that execution middleware invokes the matched handler and does not call next
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsNotNull_ShouldExecuteHandlerAndNotCallNext()
    {
        // arrange
        var handler = Substitute.For<IChosenInlineResultHandler>();
        var middleware = new ChosenInlineResultExecutionMiddleware();

        var chosenResult = new ChosenInlineResult { ResultId = "res_123", From = new User { Id = 1, FirstName = "Test" } };
        var context = new ChosenInlineResultContext(chosenResult)
        {
            MatchedHandler = handler
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
        await handler.Received(1).HandleAsync(context, TestContext.Current.CancellationToken);
        nextCalled.Should().BeFalse();
    }

    // verifies that execution middleware cleanly completes without calling next when matched handler is null
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsNull_ShouldCompleteWithoutCallingNext()
    {
        // arrange
        var middleware = new ChosenInlineResultExecutionMiddleware();
        var chosenResult = new ChosenInlineResult { ResultId = "res_123", From = new User { Id = 1, FirstName = "Test" } };
        var context = new ChosenInlineResultContext(chosenResult)
        {
            MatchedHandler = null
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
}