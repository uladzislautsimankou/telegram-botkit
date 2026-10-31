using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.ChosenInlineResults.Routing;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Tests.ChosenInlineResults;

public sealed class ChosenInlineResultRoutingMiddlewareTests
{
    private sealed class MatchingChosenInlineResultHandler : IChosenInlineResultHandler
    {
        public static bool CanHandle(ChosenInlineResult chosenResult) => true;

        public Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NonMatchingChosenInlineResultHandler : IChosenInlineResultHandler
    {
        public static bool CanHandle(ChosenInlineResult chosenResult) => false;

        public Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static ChosenInlineResultHandlerDescriptor CreateDescriptor<THandler>()
        where THandler : class, IChosenInlineResultHandler
    {
        return new ChosenInlineResultHandlerDescriptor(
            typeof(THandler),
            THandler.CanHandle,
            static sp => sp.GetRequiredService<THandler>());
    }

    // verifies that a matching handler is resolved and mapped with metadata to the context when criteria are satisfied
    [Fact]
    public async Task InvokeAsync_WhenMatchingHandlerExists_ShouldSetMatchedHandlerAndMetadataAndCallNext()
    {
        // arrange
        var handler = new MatchingChosenInlineResultHandler();

        var registry = new ChosenInlineResultHandlerRegistry();
        registry.Register(CreateDescriptor<MatchingChosenInlineResultHandler>());
        registry.Build();

        var services = new ServiceCollection();
        services.AddSingleton(handler);
        await using var provider = services.BuildServiceProvider();

        var middleware = new ChosenInlineResultRoutingMiddleware(registry, provider);
        var chosenResult = new ChosenInlineResult { ResultId = "res_123", From = new User { Id = 1, FirstName = "Test" } };
        var context = new ChosenInlineResultContext(chosenResult);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedChosenInlineResultMetadata.Should().NotBeNull();
        context.MatchedChosenInlineResultMetadata!.HandlerType.Should().Be(typeof(MatchingChosenInlineResultHandler));
        context.MatchedChosenInlineResultMetadata.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that no handler is matched and the next delegate is still executed when no handler criteria are met
    [Fact]
    public async Task InvokeAsync_WhenNoHandlersCanHandleContext_ShouldNotSetMatchedHandlerAndShouldCallNext()
    {
        // arrange
        var registry = new ChosenInlineResultHandlerRegistry();
        registry.Register(CreateDescriptor<NonMatchingChosenInlineResultHandler>());
        registry.Build();

        var services = new ServiceCollection();
        await using var provider = services.BuildServiceProvider();

        var middleware = new ChosenInlineResultRoutingMiddleware(registry, provider);
        var chosenResult = new ChosenInlineResult { ResultId = "res_123", From = new User { Id = 1, FirstName = "Test" } };
        var context = new ChosenInlineResultContext(chosenResult);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedChosenInlineResultMetadata.Should().BeNull();
    }

    // verifies that routing does not create the handler, and that the instance is created once when requested
    [Fact]
    public async Task InvokeAsync_WhenHandlerMatches_ShouldCreateItLazilyAndOnce()
    {
        // arrange
        var factoryCalls = 0;
        var registry = new ChosenInlineResultHandlerRegistry();
        registry.Register(new ChosenInlineResultHandlerDescriptor(
            typeof(MatchingChosenInlineResultHandler),
            MatchingChosenInlineResultHandler.CanHandle,
            _ => { factoryCalls++; return new MatchingChosenInlineResultHandler(); }));
        registry.Build();

        await using var provider = new ServiceCollection().BuildServiceProvider();
        var middleware = new ChosenInlineResultRoutingMiddleware(registry, provider);
        var context = new ChosenInlineResultContext(
            new ChosenInlineResult { ResultId = "res_123", From = new User { Id = 1, FirstName = "Test" } });

        // act
        await middleware.InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);
        var callsAfterRouting = factoryCalls;
        var first = context.MatchedChosenInlineResultMetadata!.HandlerInstance;
        var second = context.MatchedChosenInlineResultMetadata.HandlerInstance;

        // assert
        callsAfterRouting.Should().Be(0);
        factoryCalls.Should().Be(1);
        second.Should().BeSameAs(first);
    }
}
