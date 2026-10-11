using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Invocation;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackPipelineRegistrationTests
{
    private sealed class CancelCallbackHandler : ICallbackHandler<EmptyCallbackParams>
    {
        public static string Key => "btn:cancel";

        public Task HandleAsync(EmptyCallbackParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ConfirmCallbackHandler : ICallbackHandler<EmptyCallbackParams>
    {
        public static string Key => "btn:confirm";

        public Task HandleAsync(EmptyCallbackParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that callback handlers sharing identical parameter models register distinct invoker types without collisions
    [Fact]
    public void AddHandler_WhenMultipleHandlersShareSameParameters_ShouldRegisterDistinctInvokers()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ICallbackParameterBinder>());

        var builder = new CallbackPipelineBuilder(services);

        // act
        builder.AddHandler<CancelCallbackHandler>();
        builder.AddHandler<ConfirmCallbackHandler>();

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<CallbackHandlerRegistry>();

        // assert
        registry.TryGetInvokerType("btn:cancel", out var cancelInvokerType).Should().BeTrue();
        registry.TryGetInvokerType("btn:confirm", out var confirmInvokerType).Should().BeTrue();

        cancelInvokerType.Should().Be(typeof(CallbackHandlerInvoker<CancelCallbackHandler, EmptyCallbackParams>));
        confirmInvokerType.Should().Be(typeof(CallbackHandlerInvoker<ConfirmCallbackHandler, EmptyCallbackParams>));

        var cancelInvoker = (ICallbackHandlerInvoker)provider.GetRequiredService(cancelInvokerType!);
        var confirmInvoker = (ICallbackHandlerInvoker)provider.GetRequiredService(confirmInvokerType!);

        cancelInvoker.HandlerType.Should().Be(typeof(CancelCallbackHandler));
        confirmInvoker.HandlerType.Should().Be(typeof(ConfirmCallbackHandler));
    }
}