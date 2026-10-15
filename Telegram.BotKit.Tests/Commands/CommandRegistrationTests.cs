using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandRegistrationTests
{
    public sealed class DummyParams;

    private sealed class TestCommandHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "test";
        public static string[] Aliases => ["t", "/alias"];

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

    private sealed class ThrowingCommandHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "throw";

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("handler failure");
    }

    private sealed class SlashCommandHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "/slash";
        public static string[] Aliases => ["/s"];

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class EmptyCommandHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "   ";

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DuplicateCommandHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "test";

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class PrimaryWithAliasHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "primary";
        public static string[] Aliases => ["shared_alias", "alt_name"];

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class CommandCollidingWithAliasHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "shared_alias";

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AliasCollidingWithCommandHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "other_primary";
        public static string[] Aliases => ["primary"];

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AliasCollidingWithAliasHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "second_primary";
        public static string[] Aliases => ["shared_alias"];

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that compiled registration binds parameters and executes handler
    [Fact]
    public async Task CreateRegistration_WhenInvoked_ShouldBindParametersAndCallHandler()
    {
        // arrange
        var dummyParams = new DummyParams();
        var binder = Substitute.For<ICommandParameterBinder>();

        var services = new ServiceCollection();
        services.AddSingleton(binder);
        services.AddTransient<TestCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        var registration = CommandRegistrationHelper.CreateRegistration<TestCommandHandler, DummyParams>();
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 }, Text = "/test" };
        var context = new CommandContext(message);

        binder.Bind<DummyParams>(context).Returns(dummyParams);

        // act
        var handler = (TestCommandHandler)registration.Factory(provider);
        await registration.Invoker(handler, context, provider, TestContext.Current.CancellationToken);

        // assert
        registration.HandlerType.Should().Be(typeof(TestCommandHandler));
        handler.WasCalled.Should().BeTrue();
        handler.CapturedParameters.Should().BeSameAs(dummyParams);
        handler.CapturedContext.Should().BeSameAs(context);
        binder.Received(1).Bind<DummyParams>(context);
    }

    // verifies that exceptions thrown by handler propagate through invoker
    [Fact]
    public async Task CreateRegistration_WhenHandlerThrows_ShouldPropagateException()
    {
        // arrange
        var dummyParams = new DummyParams();
        var binder = Substitute.For<ICommandParameterBinder>();

        var services = new ServiceCollection();
        services.AddSingleton(binder);
        services.AddTransient<ThrowingCommandHandler>();

        await using var provider = services.BuildServiceProvider();

        var registration = CommandRegistrationHelper.CreateRegistration<ThrowingCommandHandler, DummyParams>();
        var message = new Message { Id = 123, Chat = new Chat { Id = 1 }, Text = "/throw" };
        var context = new CommandContext(message);

        binder.Bind<DummyParams>(context).Returns(dummyParams);

        // act
        var handler = (ThrowingCommandHandler)registration.Factory(provider);
        var act = async () => await registration.Invoker(handler, context, provider, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("handler failure");
    }

    // verifies that leading slashes are stripped from command trigger and aliases
    [Fact]
    public void RegisterHandler_WhenCommandHasLeadingSlash_ShouldTrimSlashAndRegister()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();

        // act
        CommandRegistrationHelper.RegisterHandler(services, registry, typeof(SlashCommandHandler));
        registry.Build();

        // assert
        registry.TryGetRegistration("slash", out var registration).Should().BeTrue();
        registration.Should().NotBeNull();
        registry.TryGetAliasRegistration("s", out var aliasRegistration).Should().BeTrue();
        aliasRegistration.Should().NotBeNull();
    }

    // verifies that empty or whitespace command triggers are rejected
    [Fact]
    public void RegisterHandler_WhenCommandIsEmptyOrWhitespace_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();

        // act
        var act = () => CommandRegistrationHelper.RegisterHandler(services, registry, typeof(EmptyCommandHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*declares a null or empty Command trigger*");
    }

    // verifies that registering duplicate command triggers throws exception
    [Fact]
    public void RegisterHandler_WhenDuplicateCommandRegistered_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandler(services, registry, typeof(TestCommandHandler));

        // act
        var act = () => CommandRegistrationHelper.RegisterHandler(services, registry, typeof(DuplicateCommandHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command trigger '/test' is already registered*");
    }

    // verifies that registering a command whose trigger matches an existing alias throws exception
    [Fact]
    public void RegisterHandler_WhenCommandMatchesExistingAlias_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandler(services, registry, typeof(PrimaryWithAliasHandler));

        // act
        var act = () => CommandRegistrationHelper.RegisterHandler(services, registry, typeof(CommandCollidingWithAliasHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command trigger '/shared_alias' is already registered*");
    }

    // verifies that registering an alias that matches an existing primary command trigger throws exception
    [Fact]
    public void RegisterHandler_WhenAliasMatchesExistingCommand_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandler(services, registry, typeof(PrimaryWithAliasHandler));

        // act
        var act = () => CommandRegistrationHelper.RegisterHandler(services, registry, typeof(AliasCollidingWithCommandHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command alias '/primary' is already registered*");
    }

    // verifies that registering an alias that matches an existing alias of another handler throws exception
    [Fact]
    public void RegisterHandler_WhenAliasMatchesExistingAlias_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandler(services, registry, typeof(PrimaryWithAliasHandler));

        // act
        var act = () => CommandRegistrationHelper.RegisterHandler(services, registry, typeof(AliasCollidingWithAliasHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command alias '/shared_alias' is already registered*");
    }
}
