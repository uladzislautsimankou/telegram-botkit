using FluentAssertions;
using Telegram.Bot.Types;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Messages;

public sealed class MessageExecutionMiddlewareTests
{
    private sealed class ExecutionSpyHandler : IMessageHandler
    {
        public int InvocationCount { get; private set; }
        public MessageContext? LastContext { get; private set; }

        public static bool CanHandle(MessageContext context) => true;

        public Task HandleAsync(MessageContext context, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            LastContext = context;
            return Task.CompletedTask;
        }
    }

    // verifies that execution middleware invokes the matched message handler and does not call next
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsNotNull_ShouldExecuteHandlerAndNotCallNext()
    {
        // arrange
        var handler = new ExecutionSpyHandler();
        var middleware = new MessageExecutionMiddleware();

        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
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
        handler.InvocationCount.Should().Be(1);
        handler.LastContext.Should().BeSameAs(context);
        nextCalled.Should().BeFalse();
    }

    // verifies that execution middleware cleanly completes without calling next when matched handler is null
    [Fact]
    public async Task InvokeAsync_WhenMatchedHandlerIsNull_ShouldCompleteWithoutCallingNext()
    {
        // arrange
        var middleware = new MessageExecutionMiddleware();
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
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
