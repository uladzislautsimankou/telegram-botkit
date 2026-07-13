using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackExecutionMiddlewareTests
{
    // verifies that the execution middleware invokes the matched callback invoker when it is present in the context
    [Fact]
    public async Task InvokeAsync_WhenMatchedInvokerIsNotNull_ShouldExecuteInvoker()
    {
        // arrange
        var invoker = Substitute.For<ICallbackHandlerInvoker>();
        var middleware = new CallbackExecutionMiddleware();

        var query = new CallbackQuery { Id = "1", Data = "test" };
        var context = new CallbackContext(query)
        {
            MatchedInvoker = invoker
        };

        NextDelegate next = () => Task.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next, TestContext.Current.CancellationToken);

        // assert
        await invoker.Received(1).InvokeAsync(context, TestContext.Current.CancellationToken);
    }

    // verifies that the execution middleware cleanly completes without attempting invocation when matched invoker is null
    [Fact]
    public async Task InvokeAsync_WhenMatchedInvokerIsNull_ShouldDoNothing()
    {
        // arrange
        var middleware = new CallbackExecutionMiddleware();
        var query = new CallbackQuery { Id = "1", Data = "test" };
        var context = new CallbackContext(query)
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