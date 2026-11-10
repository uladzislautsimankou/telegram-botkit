using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Binding;
using Telegram.BotKit.WidgetQueries.Models;
using Telegram.BotKit.WidgetQueries.Routing;

namespace Telegram.BotKit.Tests.WidgetQueries;

public sealed class WidgetQueryRegistrationTests
{
    public sealed class DummyParams;

    private sealed class TestWidgetQueryHandler : IWidgetQueryHandler<DummyParams>
    {
        public static string Key => "test:key";

        public bool WasCalled { get; private set; }
        public DummyParams? CapturedParameters { get; private set; }
        public WidgetQueryContext? CapturedContext { get; private set; }

        public Task HandleAsync(DummyParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            CapturedParameters = parameters;
            CapturedContext = context;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingWidgetQueryHandler : IWidgetQueryHandler<DummyParams>
    {
        public static string Key => "throw:key";

        public Task HandleAsync(DummyParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("widget query failure");
    }

    private sealed class EmptyKeyWidgetQueryHandler : IWidgetQueryHandler<DummyParams>
    {
        public static string Key => "   ";

        public Task HandleAsync(DummyParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DuplicateWidgetQueryHandler : IWidgetQueryHandler<DummyParams>
    {
        public static string Key => "test:key";

        public Task HandleAsync(DummyParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    public sealed class OtherParams;

    private sealed class NotAHandler;

    private sealed class TwoModelsWidgetQueryHandler : IWidgetQueryHandler<DummyParams>, IWidgetQueryHandler<OtherParams>
    {
        public static string Key => "two:models";

        public Task HandleAsync(DummyParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task HandleAsync(OtherParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class QuestionMarkKeyHandler : IWidgetQueryHandler<DummyParams>
    {
        public static string Key => "poll:vote?o=1";

        public Task HandleAsync(DummyParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class PaddedKeyHandler : IWidgetQueryHandler<DummyParams>
    {
        public static string Key => "  padded:key  ";

        public Task HandleAsync(DummyParams parameters, WidgetQueryContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that a type implementing the handler interface for several parameter models is rejected with a clear message
    [Fact]
    public void RegisterHandlerByType_WhenHandlerImplementsInterfaceMultipleTimes_ShouldThrowInvalidOperationException()
    {
        var act = () => WidgetQueryRegistrationHelper.RegisterHandlerByType(
            new ServiceCollection(), new WidgetQueryHandlerRegistry(), typeof(TwoModelsWidgetQueryHandler));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*implements IWidgetQueryHandler<> 2 times*");
    }

    // verifies that a type which is not a widget query handler is rejected
    [Fact]
    public void RegisterHandlerByType_WhenTypeIsNotAWidgetQueryHandler_ShouldThrowInvalidOperationException()
    {
        var act = () => WidgetQueryRegistrationHelper.RegisterHandlerByType(
            new ServiceCollection(), new WidgetQueryHandlerRegistry(), typeof(NotAHandler));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not implement IWidgetQueryHandler<TParams>*");
    }

    // verifies that keys containing the query separator are rejected because the parser would never match them
    [Fact]
    public void RegisterHandlerByType_WhenKeyContainsQuestionMark_ShouldThrowInvalidOperationException()
    {
        var act = () => WidgetQueryRegistrationHelper.RegisterHandlerByType(
            new ServiceCollection(), new WidgetQueryHandlerRegistry(), typeof(QuestionMarkKeyHandler));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must not contain '?'*");
    }

    // verifies that surrounding whitespace is trimmed from the registered key
    [Fact]
    public void RegisterHandlerByType_WhenKeyHasSurroundingWhitespace_ShouldRegisterTrimmedKey()
    {
        var registry = new WidgetQueryHandlerRegistry();

        WidgetQueryRegistrationHelper.RegisterHandlerByType(new ServiceCollection(), registry, typeof(PaddedKeyHandler));
        registry.Build();

        registry.TryGetRegistration("padded:key", out _).Should().BeTrue();
    }

    // verifies that compiled widget query registration binds parameters and executes handler
    [Fact]
    public async Task CreateRegistration_WhenInvoked_ShouldBindParametersAndCallHandler()
    {
        // arrange
        var binder = Substitute.For<IWidgetParameterBinder>();
        var dummyParams = new DummyParams();

        var services = new ServiceCollection();
        services.AddSingleton(binder);
        services.AddTransient<TestWidgetQueryHandler>();

        await using var provider = services.BuildServiceProvider();

        var context = WidgetQueryTestData.Context("test:key");

        binder.Bind<DummyParams>(context).Returns(dummyParams);

        var registration = WidgetQueryRegistrationHelper.CreateRegistration<TestWidgetQueryHandler, DummyParams>();

        // act
        var handler = (TestWidgetQueryHandler)registration.Factory(provider);
        await registration.Invoker(handler, context, provider, TestContext.Current.CancellationToken);

        // assert
        binder.Received(1).Bind<DummyParams>(context);
        handler.WasCalled.Should().BeTrue();
        handler.CapturedParameters.Should().BeSameAs(dummyParams);
        handler.CapturedContext.Should().BeSameAs(context);
        registration.HandlerType.Should().Be(typeof(TestWidgetQueryHandler));
        registration.ParamsType.Should().Be(typeof(DummyParams));
    }

    // verifies that the handler gets the context of a forwarded copy as it is
    [Fact]
    public async Task CreateRegistration_WhenContextIsCopy_ShouldPassCopyContextToHandler()
    {
        // arrange
        var binder = Substitute.For<IWidgetParameterBinder>();

        var services = new ServiceCollection();
        services.AddSingleton(binder);
        services.AddTransient<TestWidgetQueryHandler>();

        await using var provider = services.BuildServiceProvider();

        var context = new WidgetQueryContext(WidgetQueryTestData.Copy("test:key"));
        binder.Bind<DummyParams>(context).Returns(new DummyParams());

        var registration = WidgetQueryRegistrationHelper.CreateRegistration<TestWidgetQueryHandler, DummyParams>();

        // act
        var handler = (TestWidgetQueryHandler)registration.Factory(provider);
        await registration.Invoker(handler, context, provider, TestContext.Current.CancellationToken);

        // assert
        handler.CapturedContext.Should().BeSameAs(context);
        handler.CapturedContext!.IsCopy.Should().BeTrue();
    }

    // verifies that exceptions thrown by widget query handler propagate through invoker
    [Fact]
    public async Task CreateRegistration_WhenHandlerThrows_ShouldPropagateException()
    {
        // arrange
        var binder = Substitute.For<IWidgetParameterBinder>();

        var services = new ServiceCollection();
        services.AddSingleton(binder);
        services.AddTransient<ThrowingWidgetQueryHandler>();

        await using var provider = services.BuildServiceProvider();

        var context = WidgetQueryTestData.Context("throw:key");
        binder.Bind<DummyParams>(context).Returns(new DummyParams());

        var registration = WidgetQueryRegistrationHelper.CreateRegistration<ThrowingWidgetQueryHandler, DummyParams>();

        // act
        var handler = (ThrowingWidgetQueryHandler)registration.Factory(provider);
        var act = async () => await registration.Invoker(handler, context, provider, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("widget query failure");
    }

    // verifies that empty or whitespace widget query keys are rejected
    [Fact]
    public void RegisterHandlerByType_WhenKeyIsEmptyOrWhitespace_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new WidgetQueryHandlerRegistry();

        // act
        var act = () => WidgetQueryRegistrationHelper.RegisterHandlerByType(services, registry, typeof(EmptyKeyWidgetQueryHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*declares a null or empty Key trigger*");
    }

    // verifies that registering duplicate widget query keys throws exception
    [Fact]
    public void RegisterHandlerByType_WhenDuplicateKeyRegistered_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new WidgetQueryHandlerRegistry();
        WidgetQueryRegistrationHelper.RegisterHandlerByType(services, registry, typeof(TestWidgetQueryHandler));

        // act
        var act = () => WidgetQueryRegistrationHelper.RegisterHandlerByType(services, registry, typeof(DuplicateWidgetQueryHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Widget query route key 'test:key' is already registered*");
    }

    // verifies that registrations expose the handler and parameter model types for startup validation
    [Fact]
    public void GetRegistrations_AfterBuild_ShouldReturnHandlerAndParamsTypes()
    {
        var registry = new WidgetQueryHandlerRegistry();
        WidgetQueryRegistrationHelper.RegisterHandlerByType(new ServiceCollection(), registry, typeof(TestWidgetQueryHandler));
        WidgetQueryRegistrationHelper.RegisterHandlerByType(new ServiceCollection(), registry, typeof(ThrowingWidgetQueryHandler));
        registry.Build();

        var registrations = registry.GetRegistrations().ToList();

        registrations.Select(r => r.HandlerType).Should()
            .BeEquivalentTo([typeof(TestWidgetQueryHandler), typeof(ThrowingWidgetQueryHandler)]);
        registrations.Should().OnlyContain(r => r.ParamsType == typeof(DummyParams));
    }

    // verifies that registering the same handler type again is a no-op instead of a key collision
    [Fact]
    public void RegisterHandlerByType_WhenSameHandlerTypeIsRegisteredTwice_ShouldNotThrowAndKeepSingleServiceDescriptor()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new WidgetQueryHandlerRegistry();

        // act
        var act = () =>
        {
            WidgetQueryRegistrationHelper.RegisterHandlerByType(services, registry, typeof(TestWidgetQueryHandler));
            WidgetQueryRegistrationHelper.RegisterHandlerByType(services, registry, typeof(TestWidgetQueryHandler));
        };

        // assert
        act.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(TestWidgetQueryHandler)).Should().Be(1);
    }

    // verifies that the registry compares route keys ignoring case
    [Fact]
    public void RegisterHandlerByType_WhenKeyDiffersOnlyInCase_ShouldThrowInvalidOperationException()
    {
        // arrange
        var registry = new WidgetQueryHandlerRegistry();
        WidgetQueryRegistrationHelper.RegisterHandlerByType(new ServiceCollection(), registry, typeof(TestWidgetQueryHandler));
        var upperCase = WidgetQueryRegistrationHelper.CreateRegistration<DuplicateWidgetQueryHandler, DummyParams>();

        // act
        var act = () => registry.Register("TEST:KEY", upperCase);

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
    }
}
