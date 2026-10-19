using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Hosting;

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
            ParamsType: typeof(TParams),
            Factory: _ => handler,
            Invoker: static (_, _, _, _) => Task.CompletedTask
        );

        return (registration, handler);
    }

    // router with a single handler registered as "bday_set" with aliases "bday_add" and "bday_create"
    private (CommandRoutingMiddleware Middleware, CommandRegistration Registration, DummyHandler1 Handler) CreateRouter()
    {
        var (registration, handler) = CreateMockRegistration<DummyHandler1, DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], registration);
        registry.Build();

        var middleware = new CommandRoutingMiddleware(registry, _botInfo, Substitute.For<IServiceProvider>());
        return (middleware, registration, handler);
    }

    private static async Task<bool> RouteAsync(CommandRoutingMiddleware middleware, CommandContext context)
    {
        var nextCalled = false;
        await middleware.InvokeAsync(
            context,
            () => { nextCalled = true; return ValueTask.CompletedTask; },
            TestContext.Current.CancellationToken);

        return nextCalled;
    }

    // verifies that the command and each of its aliases route to the same handler and continue the pipeline
    [Theory]
    [InlineData("bday_set")]
    [InlineData("bday_add")]
    [InlineData("bday_create")]
    public async Task InvokeAsync_WhenCommandOrAliasMatches_ShouldSetMetadataAndCallNext(string commandOrAlias)
    {
        // arrange
        var (middleware, registration, handler) = CreateRouter();
        var context = CreateCommandContext(commandOrAlias, ChatType.Private);

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
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
        var (middleware, _, handler) = CreateRouter();
        var context = CreateCommandContext("bday_set", ChatType.Group, targetBotUsername: botUsernameMention);

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that a misspelled command produces suggestions in any chat type, continues the pipeline and matches no handler
    [Theory]
    [InlineData(ChatType.Private)]
    [InlineData(ChatType.Group)]
    public async Task InvokeAsync_WhenSimilarCommandExists_ShouldSetSuggestionsResultAndCallNext(ChatType chatType)
    {
        // arrange
        var (middleware, _, _) = CreateRouter();
        var context = CreateCommandContext("bady_set", chatType);

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Suggestions);
        context.RouteResult.Suggestions.Should().Contain("bday_set");
        context.MatchedRegistration.Should().BeNull();
        context.MatchedCommandMetadata.Should().BeNull();
    }

    // verifies that an unknown command in a private chat is reported as not found
    [Fact]
    public async Task InvokeAsync_WhenCommandIsUnknownInPrivateChat_ShouldSetNotFoundResultAndCallNext()
    {
        // arrange
        var (middleware, _, _) = CreateRouter();
        var context = CreateCommandContext("totally_unknown", ChatType.Private);

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.NotFound);
        context.MatchedRegistration.Should().BeNull();
    }

    // verifies that an unknown command in a group chat without explicit bot mention is ignored
    [Fact]
    public async Task InvokeAsync_WhenCommandIsUnknownInGroupChat_ShouldSetIgnoredResultAndCallNext()
    {
        // arrange
        var (middleware, _, _) = CreateRouter();
        var context = CreateCommandContext("totally_unknown", ChatType.Group);

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Ignored);
        context.MatchedRegistration.Should().BeNull();
    }

    // verifies that a command addressed to another bot is ignored even when a handler with that trigger exists
    [Fact]
    public async Task InvokeAsync_WhenCommandIsTargetedAtDifferentBot_ShouldIgnoreWithoutMatchingHandler()
    {
        // arrange
        var (middleware, _, _) = CreateRouter();
        var context = CreateCommandContext("bday_add", ChatType.Private, targetBotUsername: "OtherBot");

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Ignored);
        context.MatchedRegistration.Should().BeNull();
        context.MatchedCommandMetadata.Should().BeNull();
    }

    // verifies that a virtual route cannot bypass the target bot check
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteIsSetButCommandTargetsDifferentBot_ShouldIgnore()
    {
        // arrange
        var (middleware, _, _) = CreateRouter();
        var context = CreateCommandContext("play", ChatType.Private, targetBotUsername: "OtherBot");
        context.VirtualRoute = "bday_set";

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Ignored);
        context.MatchedRegistration.Should().BeNull();
    }

    // verifies that handlers are only created for successfully routed commands
    [Theory]
    [InlineData("bday_set", ChatType.Private, "OtherBot")]
    [InlineData("totally_unknown", ChatType.Group, null)]
    [InlineData("totally_unknown", ChatType.Private, null)]
    [InlineData("bady_set", ChatType.Private, null)]
    public async Task InvokeAsync_WhenCommandIsNotRouted_ShouldNotCreateHandler(string command, ChatType chatType, string? targetBot)
    {
        // arrange
        var factoryCalls = 0;
        var registration = new CommandRegistration(
            typeof(DummyHandler1),
            typeof(object),
            _ => { factoryCalls++; return new DummyHandler1(); },
            static (_, _, _, _) => Task.CompletedTask);

        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", [], registration);
        registry.Build();

        var middleware = new CommandRoutingMiddleware(registry, _botInfo, Substitute.For<IServiceProvider>());
        var context = CreateCommandContext(command, chatType, targetBot);

        // act
        await RouteAsync(middleware, context);

        // assert
        factoryCalls.Should().Be(0);
    }

    // verifies that a virtual route overrides the original trigger and routes successfully
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteMatches_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (middleware, registration, handler) = CreateRouter();
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set";

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that a virtual route takes precedence over the registered original trigger
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

        var middleware = new CommandRoutingMiddleware(registry, _botInfo, Substitute.For<IServiceProvider>());
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set";

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedRegistration.Should().BeSameAs(bdayRegistration);
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(bdayHandler);
    }

    // verifies that an unregistered virtual route is reported and never falls back to suggestions
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteIsUnregistered_ShouldSetVirtualRouteNotFoundResultAndCallNext()
    {
        // arrange
        var (middleware, _, _) = CreateRouter();
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "non_existent_route";

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.VirtualRouteNotFound);
        context.MatchedRegistration.Should().BeNull();
    }

    // verifies that virtual routes resolve command aliases as well
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteMatchesAlias_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (middleware, registration, handler) = CreateRouter();
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_create";

        // act
        var nextCalled = await RouteAsync(middleware, context);

        // assert
        nextCalled.Should().BeTrue();
        context.RouteResult.Status.Should().Be(CommandRouteStatus.Success);
        context.MatchedRegistration.Should().BeSameAs(registration);
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that routing does not create the handler, and that the instance is created once when requested
    [Fact]
    public async Task InvokeAsync_WhenCommandMatches_ShouldCreateHandlerLazilyAndOnce()
    {
        // arrange
        var factoryCalls = 0;
        var registration = new CommandRegistration(
            typeof(DummyHandler1),
            typeof(object),
            _ => { factoryCalls++; return new DummyHandler1(); },
            static (_, _, _, _) => Task.CompletedTask);

        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", [], registration);
        registry.Build();

        var middleware = new CommandRoutingMiddleware(registry, _botInfo, Substitute.For<IServiceProvider>());
        var context = CreateCommandContext("bday_set", ChatType.Private);

        // act
        await RouteAsync(middleware, context);
        var callsAfterRouting = factoryCalls;
        var first = context.MatchedCommandMetadata!.HandlerInstance;
        var second = context.MatchedCommandMetadata.HandlerInstance;

        // assert
        callsAfterRouting.Should().Be(0);
        factoryCalls.Should().Be(1);
        second.Should().BeSameAs(first);
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
