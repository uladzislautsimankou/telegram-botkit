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
    private sealed class StubChosenInlineResultHandler : IChosenInlineResultHandler
    {
        public bool WasHandled { get; private set; }

        public static bool CanHandle(ChosenInlineResult chosenResult) => true;

        public Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken = default)
        {
            WasHandled = true;
            return Task.CompletedTask;
        }
    }

    // verifies that execution middleware invokes the matched handler and does not call next
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsNotNull_ShouldExecuteHandlerAndNotCallNext()
    {
        // arrange
        var handler = new StubChosenInlineResultHandler();
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
        nextCalled.Should().BeFalse();
        handler.WasHandled.Should().BeTrue();
        context.MatchedHandler.Should().BeSameAs(handler);
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
