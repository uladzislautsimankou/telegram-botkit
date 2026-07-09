using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares.Messages;

public sealed class MessageRoutingMiddlewareTests
{
    // verifies that a state-specific handler is preferred and routed to when the user is in that state
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsSetAndMatchingStateHandlerExists_ShouldSetMatchedHandlerAndCallNext()
    {
        // arrange
        var stateHandler = Substitute.For<IMessageHandler>();
        stateHandler.RequiredState.Returns("waiting_for_name");
        stateHandler.CanHandle(Arg.Any<MessageContext>()).Returns(true);

        var globalHandler = Substitute.For<IMessageHandler>();
        globalHandler.RequiredState.Returns((string?)null);
        globalHandler.CanHandle(Arg.Any<MessageContext>()).Returns(true);

        var middleware = new MessageRoutingMiddleware([stateHandler, globalHandler]);

        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            UserState = "waiting_for_name"
        };
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        context.MatchedHandler.Should().BeSameAs(stateHandler);
        called.Should().BeTrue();
    }

    // verifies that routing falls back to a global handler when the user is in a state but no state-specific handler can handle the context
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsSetButNoStateHandlerCanHandle_ShouldFallbackToGlobalHandlerAndCallNext()
    {
        // arrange
        var stateHandler = Substitute.For<IMessageHandler>();
        stateHandler.RequiredState.Returns("waiting_for_name");
        stateHandler.CanHandle(Arg.Any<MessageContext>()).Returns(false);

        var globalHandler = Substitute.For<IMessageHandler>();
        globalHandler.RequiredState.Returns((string?)null);
        globalHandler.CanHandle(Arg.Any<MessageContext>()).Returns(true);

        var middleware = new MessageRoutingMiddleware([stateHandler, globalHandler]);

        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            UserState = "waiting_for_name"
        };
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        context.MatchedHandler.Should().BeSameAs(globalHandler);
        called.Should().BeTrue();
    }

    // verifies that a global handler is mapped when the user has no active state
    [Fact]
    public async Task InvokeAsync_WhenUserStateIsNullAndGlobalHandlerMatches_ShouldSetMatchedHandlerAndCallNext()
    {
        // arrange
        var stateHandler = Substitute.For<IMessageHandler>();
        stateHandler.RequiredState.Returns("waiting_for_name");
        stateHandler.CanHandle(Arg.Any<MessageContext>()).Returns(true);

        var globalHandler = Substitute.For<IMessageHandler>();
        globalHandler.RequiredState.Returns((string?)null);
        globalHandler.CanHandle(Arg.Any<MessageContext>()).Returns(true);

        var middleware = new MessageRoutingMiddleware([stateHandler, globalHandler]);

        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            UserState = null
        };
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        context.MatchedHandler.Should().BeSameAs(globalHandler);
        called.Should().BeTrue();
    }

    // verifies that no handler is matched and the pipeline is not executed further when no handler criteria are met
    [Fact]
    public async Task InvokeAsync_WhenNoHandlersCanHandleContext_ShouldNotSetMatchedHandlerAndShouldNotCallNext()
    {
        // arrange
        var globalHandler = Substitute.For<IMessageHandler>();
        globalHandler.RequiredState.Returns((string?)null);
        globalHandler.CanHandle(Arg.Any<MessageContext>()).Returns(false);

        var middleware = new MessageRoutingMiddleware([globalHandler]);

        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        context.MatchedHandler.Should().BeNull();
        called.Should().BeFalse();
    }
}