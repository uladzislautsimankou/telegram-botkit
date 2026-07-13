using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Models;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackHandlerInvokerTests
{
    public sealed class DummyParams;

    // verifies that invoke async binds parameters using the provided binder and correctly forwards execution to the target handler
    [Fact]
    public async Task InvokeAsync_WhenCalled_ShouldBindParametersAndCallHandler()
    {
        // arrange
        var handler = Substitute.For<ICallbackHandler<DummyParams>>();
        var binder = Substitute.For<ICallbackParameterBinder>();
        var dummyParams = new DummyParams();

        var query = new CallbackQuery { Id = "1", Data = "test" };
        var context = new CallbackContext(query);

        binder.Bind<DummyParams>(context).Returns(dummyParams);

        var invoker = new CallbackHandlerInvoker<DummyParams>(handler, binder);

        // act
        await invoker.InvokeAsync(context, TestContext.Current.CancellationToken);

        // assert
        binder.Received(1).Bind<DummyParams>(context);
        await handler.Received(1).HandleAsync(dummyParams, context, TestContext.Current.CancellationToken);
    }
}