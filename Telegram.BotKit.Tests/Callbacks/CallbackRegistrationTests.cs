using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Callbacks.Routing;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackRegistrationTests
{
    public sealed class DummyParams;

    private sealed class TestCallbackHandler : ICallbackHandler<DummyParams>
    {
        public static string Key => "test:key";

        public bool WasCalled { get; private set; }
        public DummyParams? CapturedParameters { get; private set; }
        public CallbackContext? CapturedContext { get; private set; }

        public Task HandleAsync(DummyParams parameters, CallbackContext context, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            CapturedParameters = parameters;
            CapturedContext = context;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingCallbackHandler : ICallbackHandler<DummyParams>
    {
        public static string Key => "throw:key";

        public Task HandleAsync(DummyParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("callback failure");
    }

    private sealed class EmptyKeyCallbackHandler : ICallbackHandler<DummyParams>
    {
        public static string Key => "   ";

        public Task HandleAsync(DummyParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DuplicateCallbackHandler : ICallbackHandler<DummyParams>
    {
        public static string Key => "test:key";

        public Task HandleAsync(DummyParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that compiled callback registration binds parameters and executes handler
    [Fact]
    public async Task CreateRegistration_WhenInvoked_ShouldBindParametersAndCallHandler()
    {
        // arrange
        var binder = Substitute.For<ICallbackParameterBinder>();
        var dummyParams = new DummyParams();

        var services = new ServiceCollection();
        services.AddSingleton(binder);
        services.AddTransient<TestCallbackHandler>();

        await using var provider = services.BuildServiceProvider();

        var query = new CallbackQuery { Id = "1", Data = "test:key" };
        var context = new CallbackContext(query);

        binder.Bind<DummyParams>(context).Returns(dummyParams);

        var registration = CallbackRegistrationHelper.CreateRegistration<TestCallbackHandler, DummyParams>();

        // act
        var handler = (TestCallbackHandler)registration.Factory(provider);
        await registration.Invoker(handler, context, provider, TestContext.Current.CancellationToken);

        // assert
        binder.Received(1).Bind<DummyParams>(context);
        handler.WasCalled.Should().BeTrue();
        handler.CapturedParameters.Should().BeSameAs(dummyParams);
        handler.CapturedContext.Should().BeSameAs(context);
        registration.HandlerType.Should().Be(typeof(TestCallbackHandler));
    }

    // verifies that exceptions thrown by callback handler propagate through invoker
    [Fact]
    public async Task CreateRegistration_WhenHandlerThrows_ShouldPropagateException()
    {
        // arrange
        var binder = Substitute.For<ICallbackParameterBinder>();
        var dummyParams = new DummyParams();

        var services = new ServiceCollection();
        services.AddSingleton(binder);
        services.AddTransient<ThrowingCallbackHandler>();

        await using var provider = services.BuildServiceProvider();

        var query = new CallbackQuery { Id = "1", Data = "throw:key" };
        var context = new CallbackContext(query);

        binder.Bind<DummyParams>(context).Returns(dummyParams);

        var registration = CallbackRegistrationHelper.CreateRegistration<ThrowingCallbackHandler, DummyParams>();

        // act
        var handler = (ThrowingCallbackHandler)registration.Factory(provider);
        var act = async () => await registration.Invoker(handler, context, provider, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("callback failure");
    }

    // verifies that empty or whitespace callback keys are rejected
    [Fact]
    public void RegisterHandler_WhenKeyIsEmptyOrWhitespace_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CallbackHandlerRegistry();

        // act
        var act = () => CallbackRegistrationHelper.RegisterHandler(services, registry, typeof(EmptyKeyCallbackHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*declares a null or empty Key trigger*");
    }

    // verifies that registering duplicate callback keys throws exception
    [Fact]
    public void RegisterHandler_WhenDuplicateKeyRegistered_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CallbackHandlerRegistry();
        CallbackRegistrationHelper.RegisterHandler(services, registry, typeof(TestCallbackHandler));

        // act
        var act = () => CallbackRegistrationHelper.RegisterHandler(services, registry, typeof(DuplicateCallbackHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Callback route key 'test:key' is already registered*");
    }
}