using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares.Messages;

public sealed class MessageExecutionMiddlewareTests
{
    // verifies that the middleware executes the matched message handler when it is present in the context
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsSet_ShouldExecuteHandler()
    {
        // arrange
        var handler = Substitute.For<IMessageHandler>();
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            MatchedHandler = handler
        };
        var middleware = new MessageExecutionMiddleware();
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        await handler.Received(1).HandleAsync(context, TestContext.Current.CancellationToken);
        called.Should().BeFalse();
    }

    // verifies that the middleware does not throw or trigger any handlers when no matched handler is present
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsNull_ShouldDoNothing()
    {
        // arrange
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message);
        var middleware = new MessageExecutionMiddleware();
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
    }
}