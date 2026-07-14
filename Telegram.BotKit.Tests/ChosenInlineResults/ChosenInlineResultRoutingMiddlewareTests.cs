using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.ChosenInlineResults.Routing;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.ChosenInlineResults;

public sealed class ChosenInlineResultRoutingMiddlewareTests
{
    private sealed class TestChosenInlineResultHandler : IChosenInlineResultHandler
    {
        private readonly bool _canHandle;
        public TestChosenInlineResultHandler(bool canHandle) => _canHandle = canHandle;

        public bool CanHandle(ChosenInlineResult chosenResult) => _canHandle;
        public Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // verifies that a matching handler is resolved and mapped to the context when criteria are satisfied
    [Fact]
    public async Task InvokeAsync_WhenMatchingHandlerExists_ShouldSetMatchedHandlerAndCallNext()
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
        context.MatchedHandler.Should().BeSameAs(handler);
        called.Should().BeTrue();
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
        context.MatchedHandler.Should().BeNull();
        called.Should().BeTrue();
    }
}