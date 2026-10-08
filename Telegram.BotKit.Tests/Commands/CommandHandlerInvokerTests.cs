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

    private sealed class TestCommandHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "test";

        public bool WasCalled { get; private set; }
        public DummyParams? CapturedParameters { get; private set; }
        public CommandContext? CapturedContext { get; private set; }

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            CapturedParameters = parameters;
            CapturedContext = context;
            return Task.CompletedTask;
        }
    }

    // verifies that invoke async binds parameters using the provided binder and correctly forwards execution to the target handler
    [Fact]
    public async Task InvokeAsync_WhenCalled_ShouldBindParametersAndCallHandler()
    {
        // arrange
        var handler = new TestCommandHandler();
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
        handler.WasCalled.Should().BeTrue();
        handler.CapturedParameters.Should().BeSameAs(dummyParams);
        handler.CapturedContext.Should().BeSameAs(context);
    }
}