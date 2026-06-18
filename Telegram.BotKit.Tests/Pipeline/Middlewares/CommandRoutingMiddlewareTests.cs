using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Configuration;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Invocation;
using Telegram.BotKit.Pipeline.Middlewares;
using static Telegram.BotKit.Tests.Pipeline.Middlewares.CommandMiddlewaresTestsMocks;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public partial class CommandRoutingMiddlewareTests
{
    private readonly MockBotInfo _botInfo = new("TestBot");
    private readonly MockCommandHandler _handler = new("bday_set", ["bday_add", "bday_create"]);
    private readonly IOptionsMonitor<TelegramBotOptions> _optionsMonitor = new MockOptionsMonitor<TelegramBotOptions>(new TelegramBotOptions());

    [Fact] // команду нашли в хендлерах, заполнили метаданные и пошли дальше
    public async Task Should_Set_Metadata_And_Call_Next_When_Found_In_Handler_Map()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("bday_set", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.True(called);
        Assert.False(_handler.WasExecuted);
        Assert.NotNull(context.MatchedCommandMetadata);
        Assert.Equal(typeof(ICommandHandler<object>), context.MatchedCommandMetadata.HandlerType);
        Assert.Same(_handler, context.MatchedCommandMetadata.HandlerInstance);
    }

    [Fact] // алиас успешно находит метаданные хендлера
    public async Task Should_Set_Metadata_When_Alias_Matches()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("bday_add", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.True(called);
        Assert.False(_handler.WasExecuted);
        Assert.NotNull(context.MatchedCommandMetadata);
        Assert.Equal(typeof(ICommandHandler<object>), context.MatchedCommandMetadata.HandlerType);
        Assert.Same(_handler, context.MatchedCommandMetadata.HandlerInstance);
    }

    [Fact] // проверяем все алиасы
    public async Task Should_Match_Multiple_Aliases_Correctly()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        foreach (var alias in new[] { "bday_set", "bday_create" })
        {
            var context = CreateCommandContext(alias, ChatType.Private);
            var called = false;
            await middleware.InvokeAsync(context, () => { called = true; return Task.CompletedTask; });

            Assert.True(called, $"Alias '{alias}' should call next");
            Assert.NotNull(context.MatchedCommandMetadata);
            Assert.Same(_handler, context.MatchedCommandMetadata.HandlerInstance);
        }
    }

    [Fact] // проверяем подсказки для похожих команд
    public async Task Should_Throw_CommandSuggestions_When_Similar_Command_Found()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("bady_set", ChatType.Private);

        var exception = await Assert.ThrowsAsync<CommandSuggestionsException>(
            () => middleware.InvokeAsync(context, () => Task.CompletedTask));

        Assert.Contains("bday_set", exception.Suggestions);
    }

    [Fact] // команда не найдена в приватном чате - выбрасываем ошибку
    public async Task Should_Throw_RouteNotFoundException_For_Unknown_Command_In_Private()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("totally_unknown", ChatType.Private);

        await Assert.ThrowsAsync<RouteNotFoundException>(
            () => middleware.InvokeAsync(context, () => Task.CompletedTask));
    }

    [Fact] // команда не найдена в групповом чате
    public async Task Should_Ignore_Unknown_Command_In_Group_Chat()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("totally_unknown", ChatType.Group);
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.False(_handler.WasExecuted);
        Assert.False(called);
    }

    [Fact] // команда предназначена для другого бота
    public async Task Should_Ignore_Command_For_Different_Bot()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("bday_add", ChatType.Private, targetBotUsername: "OtherBot");
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.False(_handler.WasExecuted);
        Assert.False(called);
    }

    [Fact] // виртуальный роут успешно находит обработчик команды
    public async Task Should_Set_Metadata_When_VirtualRoute_Matches()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set";

        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.True(called);
        Assert.False(_handler.WasExecuted);
        Assert.NotNull(context.MatchedCommandMetadata);
        Assert.Same(_handler, context.MatchedCommandMetadata.HandlerInstance);
    }

    [Fact] // виртуальный роут имеет приоритет над оригинальной командой
    public async Task Should_Prefer_VirtualRoute_Over_Original_Command()
    {
        var playHandler = new MockCommandHandler("play");
        var bdayHandler = new MockCommandHandler("bday_set");

        var playInvoker = new CommandHandlerInvoker<object>(playHandler, new MockCommandParameterBinder());
        var bdayInvoker = new CommandHandlerInvoker<object>(bdayHandler, new MockCommandParameterBinder());

        var middleware = new CommandRoutingMiddleware([playInvoker, bdayInvoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set";

        var called = false;
        await middleware.InvokeAsync(context, () => { called = true; return Task.CompletedTask; });

        Assert.True(called);
        Assert.NotNull(context.MatchedCommandMetadata);
        Assert.Same(bdayHandler, context.MatchedCommandMetadata.HandlerInstance);
    }

    [Fact] // виртуальный роут не найден - выбрасываем VirtualRouteNotFoundException
    public async Task Should_Throw_VirtualRouteNotFoundException_When_VirtualRoute_Not_Found()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "non_existent_route";

        var exception = await Assert.ThrowsAsync<VirtualRouteNotFoundException>(
            () => middleware.InvokeAsync(context, () => Task.CompletedTask));

        Assert.Equal("play", exception.Command);
        Assert.Equal("non_existent_route", exception.VirtualRoute);
    }

    [Fact] // виртуальный роут работает с алиасами
    public async Task Should_Set_Metadata_When_VirtualRoute_Matches_Alias()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, _optionsMonitor);

        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_create";

        var called = false;
        await middleware.InvokeAsync(context, () => { called = true; return Task.CompletedTask; });

        Assert.True(called);
        Assert.NotNull(context.MatchedCommandMetadata);
        Assert.Same(_handler, context.MatchedCommandMetadata.HandlerInstance);
    }

    [Fact] // игнорируемые команды из настроек молча скипаются роутером
    public async Task Should_Skip_Ignored_Command()
    {
        var optionsMonitor = new MockOptionsMonitor<TelegramBotOptions>(new TelegramBotOptions { IgnoredCommands = ["price"] });
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo, optionsMonitor);

        var context = CreateCommandContext("price", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.False(called);
        Assert.Null(context.MatchedCommandMetadata);
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