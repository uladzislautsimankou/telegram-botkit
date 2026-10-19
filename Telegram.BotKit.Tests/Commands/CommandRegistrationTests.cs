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

    public sealed class OtherParams;

    private sealed class NotAHandler;

    private sealed class TwoModelsHandler : ICommandHandler<DummyParams>, ICommandHandler<OtherParams>
    {
        public static string Command => "two_models";

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task HandleAsync(OtherParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class WhitespaceTriggerHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "two words";

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AtSignAliasHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "with_bad_alias";
        public static string[] Aliases => ["bad@alias"];

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class BlankAliasesHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "blank_aliases";
        public static string[] Aliases => ["", "   ", "/"];

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AliasEqualsCommandHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "same";
        public static string[] Aliases => ["same"];

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DuplicateAliasesHandler : ICommandHandler<DummyParams>
    {
        public static string Command => "dup_alias";
        public static string[] Aliases => ["d", "D"];

        public Task HandleAsync(DummyParams parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that a type implementing the handler interface for several parameter models is rejected with a clear message
    [Fact]
    public void RegisterHandlerByType_WhenHandlerImplementsInterfaceMultipleTimes_ShouldThrowInvalidOperationException()
    {
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(
            new ServiceCollection(), new CommandHandlerRegistry(), typeof(TwoModelsHandler));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*implements ICommandHandler<> 2 times*");
    }

    // verifies that a type which is not a command handler is rejected
    [Fact]
    public void RegisterHandlerByType_WhenTypeIsNotACommandHandler_ShouldThrowInvalidOperationException()
    {
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(
            new ServiceCollection(), new CommandHandlerRegistry(), typeof(NotAHandler));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not implement ICommandHandler<TParams>*");
    }

    // verifies that triggers which the parser could never match are rejected at registration time
    [Theory]
    [InlineData(typeof(WhitespaceTriggerHandler))]
    [InlineData(typeof(AtSignAliasHandler))]
    public void RegisterHandlerByType_WhenTriggerContainsWhitespaceOrAtSign_ShouldThrowInvalidOperationException(Type handlerType)
    {
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(
            new ServiceCollection(), new CommandHandlerRegistry(), handlerType);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must not contain whitespace or '@'*");
    }

    // verifies that blank aliases are ignored instead of being registered as empty triggers
    [Fact]
    public void RegisterHandlerByType_WhenAliasesAreBlank_ShouldIgnoreThem()
    {
        var registry = new CommandHandlerRegistry();

        CommandRegistrationHelper.RegisterHandlerByType(new ServiceCollection(), registry, typeof(BlankAliasesHandler));
        registry.Build();

        registry.TryGetRegistration("blank_aliases", out _).Should().BeTrue();
        registry.TryGetAliasRegistration("", out _).Should().BeFalse();
    }

    // verifies that an alias equal to the handler's own command is reported as a collision
    [Fact]
    public void RegisterHandlerByType_WhenAliasEqualsOwnCommand_ShouldThrowInvalidOperationException()
    {
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(
            new ServiceCollection(), new CommandHandlerRegistry(), typeof(AliasEqualsCommandHandler));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command alias '/same' of 'AliasEqualsCommandHandler' is the same as its own command '/same'*");
    }

    // verifies that the same alias listed twice by one handler is reported as such, not as a clash with another handler
    [Fact]
    public void RegisterHandlerByType_WhenAliasIsListedTwice_ShouldThrowInvalidOperationException()
    {
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(
            new ServiceCollection(), new CommandHandlerRegistry(), typeof(DuplicateAliasesHandler));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command alias '/D' is listed more than once in Aliases of 'DuplicateAliasesHandler'*");
    }

    // verifies that a handler rejected because of its own declaration leaves nothing behind in the registry
    [Theory]
    [InlineData(typeof(AliasEqualsCommandHandler), "same")]
    [InlineData(typeof(DuplicateAliasesHandler), "dup_alias")]
    public void RegisterHandlerByType_WhenOwnDeclarationIsInvalid_ShouldNotRegisterAnything(Type handlerType, string command)
    {
        var registry = new CommandHandlerRegistry();

        var act = () => CommandRegistrationHelper.RegisterHandlerByType(new ServiceCollection(), registry, handlerType);
        act.Should().Throw<InvalidOperationException>();
        registry.Build();

        registry.TryGetRegistration(command, out _).Should().BeFalse();
    }

    // verifies that a handler whose alias is taken by another handler is not registered half-way
    [Fact]
    public void RegisterHandlerByType_WhenAliasIsTakenByAnotherHandler_ShouldNotRegisterItsCommand()
    {
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(PrimaryWithAliasHandler));

        var act = () => CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(AliasCollidingWithAliasHandler));
        act.Should().Throw<InvalidOperationException>();
        registry.Build();

        registry.TryGetRegistration("second_primary", out _).Should().BeFalse();
    }

    // verifies that the registrations are listed once per command, without repeating aliases
    [Fact]
    public void GetRegistrations_WhenHandlerHasAliases_ShouldReturnOneRegistrationPerCommand()
    {
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandlerByType(new ServiceCollection(), registry, typeof(PrimaryWithAliasHandler));
        CommandRegistrationHelper.RegisterHandlerByType(new ServiceCollection(), registry, typeof(DuplicateCommandHandler));
        registry.Build();

        var registrations = registry.GetRegistrations().ToList();

        registrations.Select(r => r.HandlerType).Should()
            .BeEquivalentTo([typeof(PrimaryWithAliasHandler), typeof(DuplicateCommandHandler)]);
        registrations.Should().OnlyContain(r => r.ParamsType == typeof(DummyParams));
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
    public void RegisterHandlerByType_WhenCommandHasLeadingSlash_ShouldTrimSlashAndRegister()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();

        // act
        CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(SlashCommandHandler));
        registry.Build();

        // assert
        registry.TryGetRegistration("slash", out var registration).Should().BeTrue();
        registration.Should().NotBeNull();
        registry.TryGetAliasRegistration("s", out var aliasRegistration).Should().BeTrue();
        aliasRegistration.Should().NotBeNull();
    }

    // verifies that empty or whitespace command triggers are rejected
    [Fact]
    public void RegisterHandlerByType_WhenCommandIsEmptyOrWhitespace_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();

        // act
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(EmptyCommandHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*declares a null or empty Command trigger*");
    }

    // verifies that registering duplicate command triggers throws exception
    [Fact]
    public void RegisterHandlerByType_WhenDuplicateCommandRegistered_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(TestCommandHandler));

        // act
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(DuplicateCommandHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command trigger '/test' of 'DuplicateCommandHandler' is already registered by 'TestCommandHandler'*");
    }

    // verifies that registering a command whose trigger matches an existing alias throws exception
    [Fact]
    public void RegisterHandlerByType_WhenCommandMatchesExistingAlias_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(PrimaryWithAliasHandler));

        // act
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(CommandCollidingWithAliasHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command trigger '/shared_alias' of 'CommandCollidingWithAliasHandler' is already registered by 'PrimaryWithAliasHandler'*");
    }

    // verifies that registering an alias that matches an existing primary command trigger throws exception
    [Fact]
    public void RegisterHandlerByType_WhenAliasMatchesExistingCommand_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(PrimaryWithAliasHandler));

        // act
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(AliasCollidingWithCommandHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command alias '/primary' of 'AliasCollidingWithCommandHandler' is already registered by 'PrimaryWithAliasHandler'*");
    }

    // verifies that registering an alias that matches an existing alias of another handler throws exception
    [Fact]
    public void RegisterHandlerByType_WhenAliasMatchesExistingAlias_ShouldThrowInvalidOperationException()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();
        CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(PrimaryWithAliasHandler));

        // act
        var act = () => CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(AliasCollidingWithAliasHandler));

        // assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Command alias '/shared_alias' of 'AliasCollidingWithAliasHandler' is already registered by 'PrimaryWithAliasHandler'*");
    }

    // verifies that registering the same handler type again is a no-op instead of a trigger collision
    [Fact]
    public void RegisterHandlerByType_WhenSameHandlerTypeIsRegisteredTwice_ShouldNotThrowAndKeepSingleServiceDescriptor()
    {
        // arrange
        var services = new ServiceCollection();
        var registry = new CommandHandlerRegistry();

        // act
        var act = () =>
        {
            CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(TestCommandHandler));
            CommandRegistrationHelper.RegisterHandlerByType(services, registry, typeof(TestCommandHandler));
        };

        // assert
        act.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(TestCommandHandler)).Should().Be(1);
    }
}
