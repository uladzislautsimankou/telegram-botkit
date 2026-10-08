using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Invocation;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Commands.Routing;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared;
using Telegram.BotKit.Shared.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Telegram.BotKit.Tests.Commands;

public sealed class CommandRoutingMiddlewareTests
{
    private readonly IBotInfo _botInfo;
    private readonly IOptionsMonitor<TelegramBotOptions> _optionsMonitor;

    private sealed class DummyParams1;
    private sealed class DummyParams2;

    private sealed class DummyHandler;

    public CommandRoutingMiddlewareTests()
    {
        _botInfo = Substitute.For<IBotInfo>();
        _botInfo.Username.Returns("TestBot");

        _optionsMonitor = Substitute.For<IOptionsMonitor<TelegramBotOptions>>();
        _optionsMonitor.CurrentValue.Returns(new TelegramBotOptions());
    }

    private IServiceProvider CreateMockServiceProvider(Dictionary<Type, object> services)
    {
        var provider = Substitute.For<IServiceProvider>();

        foreach (var (type, instance) in services)
        {
            provider.GetService(type).Returns(instance);
        }

        return provider;
    }

    private (Type InvokerType, ICommandHandlerInvoker Invoker, object Handler) CreateMockInvoker<TParams>()
        where TParams : class, new()
    {
        var handler = new DummyHandler();
        var invoker = Substitute.For<ICommandHandlerInvoker>();
        invoker.HandlerType.Returns(typeof(DummyHandler));
        invoker.HandlerInstance.Returns(handler);

        return (typeof(CommandHandlerInvoker<TParams>), invoker, handler);
    }

    // verifies that the router populates matched command metadata and invokes the next pipeline step when a matching handler is registered
    [Fact]
    public async Task InvokeAsync_WhenCommandIsFoundInHandlerMap_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (invokerType, invoker, handler) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);

        var context = CreateCommandContext("bday_set", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerType.Should().Be(typeof(DummyHandler));
        context.MatchedCommandMetadata.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that aliases map successfully to the primary handler metadata and invoke the next delegate
    [Fact]
    public async Task InvokeAsync_WhenCommandMatchesAlias_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (invokerType, invoker, handler) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);

        var context = CreateCommandContext("bday_add", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerType.Should().Be(typeof(DummyHandler));
        context.MatchedCommandMetadata.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that multiple registered aliases route to the same command handler correctly
    [Fact]
    public async Task InvokeAsync_WhenMultipleCommandAliasesAreProvided_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (invokerType, invoker, handler) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);

        // act & assert
        foreach (var alias in new[] { "bday_set", "bday_create" })
        {
            var context = CreateCommandContext(alias, ChatType.Private);
            var called = false;
            await middleware.InvokeAsync(context, () => { called = true; return Task.CompletedTask; }, TestContext.Current.CancellationToken);

            called.Should().BeTrue($"Alias '{alias}' should call next");
            context.MatchedCommandMetadata.Should().NotBeNull();
            context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(handler);
        }
    }

    // verifies that entering a misspelled command trigger throws a suggestion exception populated with potential matching commands
    [Fact]
    public async Task InvokeAsync_WhenSimilarCommandExists_ShouldThrowCommandSuggestionsException()
    {
        // arrange
        var (invokerType, invoker, _) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);
        var context = CreateCommandContext("bady_set", ChatType.Private);

        // act
        var act = () => middleware.InvokeAsync(context, () => Task.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        var exceptionAssertion = await act.Should().ThrowAsync<CommandSuggestionsException>();
        exceptionAssertion.Which.Suggestions.Should().Contain("bday_set");
    }

    // verifies that an unknown command trigger in a private chat context immediately throws a route not found exception
    [Fact]
    public async Task InvokeAsync_WhenCommandIsUnknownInPrivateChat_ShouldThrowRouteNotFoundException()
    {
        // arrange
        var (invokerType, invoker, _) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);
        var context = CreateCommandContext("totally_unknown", ChatType.Private);

        // act
        var act = () => middleware.InvokeAsync(context, () => Task.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        await act.Should().ThrowAsync<RouteNotFoundException>();
    }

    // verifies that unrecognized commands inside non-private group chats are ignored without throwing exceptions or calling next
    [Fact]
    public async Task InvokeAsync_WhenCommandIsUnknownInGroupChat_ShouldIgnoreAndShortCircuit()
    {
        // arrange
        var (invokerType, invoker, _) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);
        var context = CreateCommandContext("totally_unknown", ChatType.Group);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
    }

    // verifies that commands with bot mention suffixes targeting other bot usernames are ignored silently
    [Fact]
    public async Task InvokeAsync_WhenCommandIsTargetedAtDifferentBot_ShouldIgnoreAndShortCircuit()
    {
        // arrange
        var (invokerType, invoker, _) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);
        var context = CreateCommandContext("bday_add", ChatType.Private, targetBotUsername: "OtherBot");
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
    }

    // verifies that context containing a valid virtual route overrides the default command trigger and routes successfully
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteMatches_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (invokerType, invoker, handler) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set";
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that a virtual route trigger takes precedence over the original physical command trigger in routing resolution
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteIsProvided_ShouldPreferVirtualRouteOverOriginalCommand()
    {
        // arrange
        var (playInvokerType, playInvoker, _) = CreateMockInvoker<DummyParams1>();
        var (bdayInvokerType, bdayInvoker, bdayHandler) = CreateMockInvoker<DummyParams2>();

        var registry = new CommandHandlerRegistry();
        registry.Register("play", [], playInvokerType);
        registry.Register("bday_set", [], bdayInvokerType);

        var serviceProvider = CreateMockServiceProvider(new()
        {
            [playInvokerType] = playInvoker,
            [bdayInvokerType] = bdayInvoker
        });

        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set";
        var called = false;

        // act
        await middleware.InvokeAsync(context, () => { called = true; return Task.CompletedTask; }, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(bdayHandler);
    }

    // verifies that specifying an unregistered virtual route throws a dedicated virtual route not found exception
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteIsUnregistered_ShouldThrowVirtualRouteNotFoundException()
    {
        // arrange
        var (invokerType, invoker, _) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "non_existent_route";

        // act
        var act = () => middleware.InvokeAsync(context, () => Task.CompletedTask, TestContext.Current.CancellationToken);

        // assert
        var exceptionAssertion = await act.Should().ThrowAsync<VirtualRouteNotFoundException>();
        exceptionAssertion.Which.Command.Should().Be("play");
        exceptionAssertion.Which.VirtualRoute.Should().Be("non_existent_route");
    }

    // verifies that virtual routes are compatible with command aliases during routing resolution
    [Fact]
    public async Task InvokeAsync_WhenVirtualRouteMatchesAlias_ShouldSetMetadataAndCallNext()
    {
        // arrange
        var (invokerType, invoker, handler) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, _optionsMonitor, serviceProvider);
        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_create";
        var called = false;

        // act
        await middleware.InvokeAsync(context, () => { called = true; return Task.CompletedTask; }, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeTrue();
        context.MatchedCommandMetadata.Should().NotBeNull();
        context.MatchedCommandMetadata!.HandlerInstance.Should().BeSameAs(handler);
    }

    // verifies that commands added to the bot configuration's ignore list are skipped without pipeline progression or metadata setting
    [Fact]
    public async Task InvokeAsync_WhenCommandIsInIgnoreList_ShouldSkipAndShortCircuit()
    {
        // arrange
        var optionsMonitor = Substitute.For<IOptionsMonitor<TelegramBotOptions>>();
        optionsMonitor.CurrentValue.Returns(new TelegramBotOptions { IgnoredCommands = ["price"] });

        var (invokerType, invoker, _) = CreateMockInvoker<DummyParams1>();
        var registry = new CommandHandlerRegistry();
        registry.Register("bday_set", ["bday_add", "bday_create"], invokerType);

        var serviceProvider = CreateMockServiceProvider(new() { [invokerType] = invoker });
        var middleware = new CommandRoutingMiddleware(registry, _botInfo, optionsMonitor, serviceProvider);
        var context = CreateCommandContext("price", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = () => { called = true; return Task.CompletedTask; };

        // act
        await middleware.InvokeAsync(context, nextDelegate, TestContext.Current.CancellationToken);

        // assert
        called.Should().BeFalse();
        context.MatchedCommandMetadata.Should().BeNull();
    }

    private CommandContext CreateCommandContext(
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