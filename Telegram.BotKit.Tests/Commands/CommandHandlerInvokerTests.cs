using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Models;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandHandlerInvokerTests
{
    public sealed class DummyParams;

    // verifies that invoke async binds parameters using the provided binder and correctly forwards execution to the target handler
    [Fact]
    public async Task InvokeAsync_WhenCalled_ShouldBindParametersAndCallHandler()
    {
        // arrange
        var handler = Substitute.For<ICommandHandler<DummyParams>>();
        var binder = Substitute.For<ICommandParameterBinder>();
        var dummyParams = new DummyParams();

        var message = new Message { Id = 123, Chat = new Chat { Id = 1 } };
        var context = new CommandContext(message);

        binder.Bind<DummyParams>(context).Returns(dummyParams);

        var invoker = new CommandHandlerInvoker<DummyParams>(handler, binder);

        // act
        await invoker.InvokeAsync(context, TestContext.Current.CancellationToken);

        // assert
        binder.Received(1).Bind<DummyParams>(context);
        await handler.Received(1).HandleAsync(dummyParams, context, TestContext.Current.CancellationToken);
    }
}