using FluentAssertions;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.ChosenInlineResults.Routing;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.ChosenInlineResults;

public sealed class ChosenInlineResultRoutingMiddlewareTests
{
    private sealed class TestChosenInlineResultHandler(bool canHandle) : IChosenInlineResultHandler
    {
        public bool CanHandle(ChosenInlineResult chosenResult) => canHandle;

        public Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // verifies that a matching handler is resolved and mapped with metadata to the context when criteria are satisfied
    [Fact]
    public async Task InvokeAsync_WhenMatchingHandlerExists_ShouldSetMatchedHandlerAndMetadataAndCallNext()
    {
        // arrange
        var handler = new TestChosenInlineResultHandler(canHandle: true);
        var middleware = new ChosenInlineResultRoutingMiddleware([handler]);

        var chosenResult = new ChosenInlineResult { ResultId = "res_123", From = new User { Id = 1, FirstName = "Test" } };
        var context = new ChosenInlineResultContext(chosenResult);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedHandler.Should().BeSameAs(handler);
        context.MatchedChosenInlineResultMetadata.Should().NotBeNull();
        context.MatchedChosenInlineResultMetadata!.HandlerType.Should().Be(typeof(TestChosenInlineResultHandler));
        context.MatchedChosenInlineResultMetadata.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that no handler is matched and the next delegate is still executed when no handler criteria are met
    [Fact]
    public async Task InvokeAsync_WhenNoHandlersCanHandleContext_ShouldNotSetMatchedHandlerAndShouldCallNext()
    {
        // arrange
        var handler = new TestChosenInlineResultHandler(canHandle: false);
        var middleware = new ChosenInlineResultRoutingMiddleware([handler]);

        var chosenResult = new ChosenInlineResult { ResultId = "res_123", From = new User { Id = 1, FirstName = "Test" } };
        var context = new ChosenInlineResultContext(chosenResult);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedHandler.Should().BeNull();
        context.MatchedChosenInlineResultMetadata.Should().BeNull();
    }
}