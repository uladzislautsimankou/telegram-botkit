using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.BotKit.Callbacks.Abstractions;
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

    private sealed class IdempotentCallbackHandler : ICallbackHandler<EmptyCallbackParams>
    {
        public static string Key => "idem";

        public Task HandleAsync(EmptyCallbackParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that callback handlers sharing identical parameter models register distinct registrations and factories without collisions
    [Fact]
    public async Task AddHandler_WhenMultipleHandlersShareSameParameters_ShouldRegisterDistinctRegistrations()
    {
        // arrange
        var services = new ServiceCollection();

        var registry = new CallbackHandlerRegistry();
        var builder = new CallbackPipelineBuilder(services, registry);

        // act
        builder.AddHandler<CancelCallbackHandler>();
        builder.AddHandler<ConfirmCallbackHandler>();

        await using var provider = services.BuildServiceProvider();
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
        var builder = new CallbackPipelineBuilder(services, new CallbackHandlerRegistry());

        // act
        builder.UseRouting();
        builder.UseRouting();

        // assert
        builder.Steps.Should().ContainSingle(step => step.Type == PipelineStepType.Routing);
    }

    // verifies that adding the same handler twice is harmless
    [Fact]
    public void AddHandler_WhenSameHandlerAddedTwice_ShouldNotThrowAndRegisterOnce()
    {
        var services = new ServiceCollection();
        var builder = new CallbackPipelineBuilder(services, new CallbackHandlerRegistry());

        var act = () =>
        {
            builder.AddHandler<IdempotentCallbackHandler>();
            builder.AddHandler<IdempotentCallbackHandler>();
        };

        act.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(IdempotentCallbackHandler)).Should().Be(1);
    }
}
