using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandRoutingMiddlewareTests
{
    private readonly IBotInfo _botInfo;

    private sealed class DummyParams1;
    private sealed class DummyParams2;

    private sealed class DummyHandler1 : ICommandHandler<DummyParams1>
    {
        public static string Command => "dummy1";
        public Task HandleAsync(DummyParams1 parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class DummyHandler2 : ICommandHandler<DummyParams2>
    {
        public static string Command => "dummy2";
        public Task HandleAsync(DummyParams2 parameters, CommandContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    public CommandRoutingMiddlewareTests()
    {
        _botInfo = Substitute.For<IBotInfo>();
        _botInfo.Username.Returns("TestBot");
    }

    private static (CommandRegistration Registration, THandler Handler) CreateMockRegistration<THandler, TParams>()
        where THandler : class, ICommandHandler<TParams>, new()
        where TParams : class, new()
    {
        var handler = new THandler();
        var registration = new CommandRegistration(
            HandlerType: typeof(THandler),
            Factory: _ => handler,
            Invoker: static (_, _, _, _) => Task.CompletedTask
        );

        return (registration, handler);
    }

    // verifies that the router populates matched metadata and invokes next when handler is registered
    [Fact]
    public async Task InvokeAsync_WhenCommandIsFoundInHandlerMap_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (registration, handler) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);

        var context = CreateCommandContext("bday_set", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerType.Should().Be(typeof(DummyHandler1));
        context.MatchedCommandMetadata.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that multiple registered aliases route to the same command handler correctly
    [Theory]
    [InlineData("bday_set")]
    [InlineData("bday_add")]
    [InlineData("bday_create")]
    public async Task InvokeAsync_WhenCommandMatchesAlias_ShouldSetMetadataAndCallNext(string commandOrAlias)
    {
        // arrange
        var (registration, handler) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);

        var context = CreateCommandContext(commandOrAlias, ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerType.Should().Be(typeof(DummyHandler1));
        context.MatchedCommandMetadata.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that commands targeting our bot explicitly are accepted and routed
    [Theory]
    [InlineData("TestBot")]
    [InlineData("testbot")]
    public async Task InvokeAsync_WhenCommandTargetsOurBotUsername_ShouldSetSuccessAndCallNext(string botUsernameMention)
    {
        // arrange
        var (registration, handler) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("bday_set", ChatType.Group, targetBotUsername: botUsernameMention);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that entering a misspelled command sets suggestions route result and short-circuits
    [Fact]
    public async Task InvokeAsync_WhenSimilarCommandExists_ShouldSetSuggestionsResultAndShortCircuit()
    {
        // arrange
        var (registration, _) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("bady_set", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Suggestions);
        context.RouteResult.Suggestions.Should().Contain("bday_set");
    }

    // verifies that misspelled commands inside group chats set suggestions route result and short-circuits
    [Fact]
    public async Task InvokeAsync_WhenSimilarCommandExistsInGroupChat_ShouldSetSuggestionsResultAndShortCircuit()
    {
        // arrange
        var (registration, _) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("bady_set", ChatType.Group);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Suggestions);
        context.RouteResult.Suggestions.Should().Contain("bday_set");
    }

    // verifies that an unknown command trigger in a private chat sets not found route result and short-circuits
    [Fact]
    public async Task InvokeAsync_WhenCommandIsUnknownInPrivateChat_ShouldSetNotFoundResultAndShortCircuit()
    {
        // arrange
        var (registration, _) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("totally_unknown", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.NotFound);
    }

    // verifies that unrecognized commands inside non-private group chats are ignored without calling next
    [Fact]
    public async Task InvokeAsync_WhenCommandIsUnknownInGroupChat_ShouldIgnoreAndShortCircuit()
    {
        // arrange
        var (registration, _) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("totally_unknown", ChatType.Group);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Ignored);
    }

    // verifies that commands with bot mention suffixes targeting other bot usernames are ignored silently
    [Fact]
    public async Task InvokeAsync_WhenCommandIsTargetedAtDifferentBot_ShouldIgnoreAndShortCircuit()
    {
        // arrange
        var (registration, _) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("bday_add", ChatType.Private, targetBotUsername: "OtherBot");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Ignored);
    }

    // verifies that context containing a valid virtual route overrides default trigger and routes successfully
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteMatches_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (registration, handler) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set";
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that a virtual route trigger takes precedence over original physical trigger in routing
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteIsProvided_ShouldPreferVirtualRouteOverOriginalCommand()
    {
        // arrange
        var (playRegistration, _) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var (bdayRegistration, bdayHandler) = CreateMockRegistration<DummyHandler2, DummyParams2>();

        var registry = new CommandHandlerRegistry();
        registry.Register("play", [], playRegistration);
        registry.Register("bday_set", [], bdayRegistration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set";
        var called = false;

        // act
        await middleware.InvokeAsync(context, () => { called = true; return ValueTask.CompletedTask; }, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedRegistration.Should().BeSameAs(bdayRegistration);
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(bdayHandler);
    }

    // verifies that specifying an unregistered virtual route sets virtual route not found result and short-circuits
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteIsUnregistered_ShouldSetVirtualRouteNotFoundResultAndShortCircuit()
    {
        // arrange
        var (registration, _) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "non_existent_route";
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return ValueTask.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.VirtualRouteNotFound);
    }

    // verifies that virtual routes are compatible with command aliases during routing resolution
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteMatchesAlias_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (registration, handler) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var serviceProvider = Substitute.For<IServiceProvider>();
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, serviceProvider);
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_create";
        var called = false;

        // act
        await middleware.InvokeAsync(context, () => { called = true; return ValueTask.CompletedTask; }, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    private static CommandContext CreateCommandContext(
        string command,
        ChatType chatType,
        string? targetBotUsername = null)
    {
        var text = targetBotUsername != null
            ? $"/{command}@{targetBotUsername}"
            : $"/{command}";

        var message = new Message
        {
            Chat = new Chat { Id = 123, Type = chatType },
            From = new User { Id = 456, FirstName = "Test", IsBot = false },
            Text = text
        };

        return new CommandContext(message);
    }
}