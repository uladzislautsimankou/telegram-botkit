using FluentAssertions;
using Telegram.Bot.Types;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared.Pipeline;

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
    public async Task InvokeAsync_WhenHandlerWasMatched_ShouldExecuteHandlerAndNotCallNext()
    {
        // arrange
        var handler = new ExecutionSpyHandler();
        var middleware = new MessageExecutionMiddleware();

        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new MessageContext(message)
        {
            MatchedMessageMetadata = new MessageMetadata(typeof(ExecutionSpyHandler), () => handler)
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

    // verifies that execution middleware cleanly completes without calling next when no handler was matched
    [Fact]
    public async Task InvokeAsync_WhenNoHandlerWasMatched_ShouldCompleteWithoutCallingNext()
    {
        // arrange
        var middleware = new MessageExecutionMiddleware();
        var context = new MessageContext(new Message { Id = 123, Chat = new Chat { Id = 1 } });

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

    // verifies that the handler is created only when execution runs, and exactly once
    [Fact]
    public async Task InvokeAsync_WhenExecuting_ShouldCreateHandlerExactlyOnce()
    {
        // arrange
        var handler = new ExecutionSpyHandler();
        var factoryCalls = 0;
        var context = new MessageContext(new Message { Id = 123, Chat = new Chat { Id = 1 } })
        {
            MatchedMessageMetadata = new MessageMetadata(typeof(ExecutionSpyHandler), () => { factoryCalls++; return handler; })
        };

        factoryCalls.Should().Be(0);

        // act
        await new MessageExecutionMiddleware().InvokeAsync(context, () => ValueTask.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        factoryCalls.Should().Be(1);
        handler.InvocationCount.Should().Be(1);
    }
}
