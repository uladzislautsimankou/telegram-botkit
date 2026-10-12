using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;
using Telegram.BotKit.Shared.Pipeline;

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

    // verifies that callback handlers sharing identical parameter models register distinct registrations and factories without collisions
    [Fact]
    public async Task AddHandler_WhenMultipleHandlersShareSameParameters_ShouldRegisterDistinctRegistrations()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ICallbackParameterBinder>());

        var builder = new CallbackPipelineBuilder(services);

        // act
        builder.AddHandler<CancelCallbackHandler>();
        builder.AddHandler<ConfirmCallbackHandler>();

        await using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<CallbackHandlerRegistry>();
        registry.Build();

        // assert
        registry.TryGetRegistration("btn:cancel", out var cancelRegistration).Should().BeTrue();
        registry.TryGetRegistration("btn:confirm", out var confirmRegistration).Should().BeTrue();

        cancelRegistration!.HandlerType.Should().Be(typeof(CancelCallbackHandler));
        confirmRegistration!.HandlerType.Should().Be(typeof(ConfirmCallbackHandler));

        var cancelInstance = cancelRegistration.Factory(provider);
        var confirmInstance = confirmRegistration.Factory(provider);

        cancelInstance.Should().BeOfType<CancelCallbackHandler>();
        confirmInstance.Should().BeOfType<ConfirmCallbackHandler>();
    }

    // verifies that multiple calls to userouting do not introduce duplicate steps
    [Fact]
    public void UseRouting_WhenCalledMultipleTimes_ShouldRegisterOnlySingleStep()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = new CallbackPipelineBuilder(services);

        // act
        builder.UseRouting();
        builder.UseRouting();

        // assert
        builder.Steps.Should().ContainSingle(step => step.Type == PipelineStepType.Routing);
    }
}