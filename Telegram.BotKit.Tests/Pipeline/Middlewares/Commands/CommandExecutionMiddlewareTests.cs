using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Invocation;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares.Commands;

public sealed class CommandExecutionMiddlewareTests
{
    // verifies that a command executes its matched handler and short-circuits the pipeline when a matched invoker is set in the context
    [Fact]
    public async Task InvokeAsync_WhenMatchedInvokerIsSet_ShouldExecuteCommandHandlerAndShortCircuit()
    {
        // arrange
        var invoker = Substitute.For<ICommandHandlerInvoker>();
        var middleware = new CommandExecutionMiddleware();
        var context = CreateCommandContext("bday_set", ChatType.Private);
        context.MatchedInvoker = invoker;

        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        await invoker.Received(1).InvokeAsync(context, TestContext.Current.CancellationToken);
        called.Should().BeFalse();
    }

    // verifies that the middleware does not execute any handler or invoke subsequent delegates when no matched invoker is set
    [Fact]
    public async Task InvokeAsync_WhenMatchedInvokerIsNull_ShouldDoNothing()
    {
        // arrange
        var middleware = new CommandExecutionMiddleware();
        var context = CreateCommandContext("bday_set", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
    }

    private CommandContext CreateCommandContext(string command, ChatType chatType)
    {
        var message = new Bot.Types.Message
        {
            Chat = new Bot.Types.Chat { Id = 123, Type = chatType },
            From = new Bot.Types.User { Id = 456, FirstName = "Test" },
            Text = $"/{command}"
        };

        return new CommandContext(message);
    }
}