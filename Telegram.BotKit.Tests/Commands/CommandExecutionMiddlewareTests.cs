using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandExecutionMiddlewareTests
{
    // verifies that the execution middleware invokes the matched command invoker when it is present in the context
    [Fact]
    public async Task InvokeAsync_WhenMatchedInvokerIsNotNull_ShouldExecuteInvoker()
    {
        // arrange
        var invoker = Substitute.For<ICallbackHandlerInvoker>(); // Wait, let's use ICommandHandlerInvoker
        var commandInvoker = Substitute.For<ICommandHandlerInvoker>();
        var middleware = new CommandExecutionMiddleware();

        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new CommandContext(message)
        {
            MatchedInvoker = commandInvoker
        };

        NextDelegate next = () => Task.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        await commandInvoker.Received(1).InvokeAsync(context, TestContext.Current.CancellationToken);
    }

    // verifies that the execution middleware cleanly completes without attempting invocation when matched invoker is null
    [Fact]
    public async Task InvokeAsync_WhenMatchedInvokerIsNull_ShouldDoNothing()
    {
        // arrange
        var middleware = new CommandExecutionMiddleware();
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new CommandContext(message)
        {
            MatchedInvoker = null
        };

        NextDelegate next = () => Task.CompletedTask;

        // act
        var act = () => middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        await act.Should().NotThrowAsync();
    }
}