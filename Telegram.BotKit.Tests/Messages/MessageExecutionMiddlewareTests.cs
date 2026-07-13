using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Messages;

public sealed class MessageExecutionMiddlewareTests
{
    // verifies that the execution middleware invokes the matched message handler when it is present in the context
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsNotNull_ShouldExecuteHandler()
    {
        // arrange
        var handler = Substitute.For<IMessageHandler>();
        var middleware = new MessageExecutionMiddleware();

        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
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
        var middleware = new MessageExecutionMiddleware();
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
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