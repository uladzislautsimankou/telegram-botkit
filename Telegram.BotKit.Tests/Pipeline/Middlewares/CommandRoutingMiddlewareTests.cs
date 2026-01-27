using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Invocation;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public partial class CommandRoutingMiddlewareTests
{
    private readonly MockBotInfo _botInfo = new("TestBot");
    private readonly MockCommandHandler _handler = new("bday_set", ["bday_add", "bday_create"]);

    [Fact] // обычное поведение, команду нашли и выполнили
    public async Task Should_Execute_Command_When_Found_In_Handler_Map()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

        var context = CreateCommandContext("bday_set", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.True(_handler.WasExecuted);
        Assert.False(called);
    }

    [Fact] // проверяем, что алиас тоже срабатывает
    public async Task Should_Execute_Original_Command_When_Alias_Matches()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

        var context = CreateCommandContext("bday_add", ChatType.Private);
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.True(_handler.WasExecuted);
        Assert.False(called);
    }

    [Fact] // проверяем все алиасы
    public async Task Should_Execute_Multiple_Aliases_Correctly()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

        foreach (var alias in new[] { "bday_set", "bday_create" })
        {
            _handler.ResetExecution();
            var context = CreateCommandContext(alias, ChatType.Private);
            await middleware.InvokeAsync(context, () => Task.CompletedTask);
            Assert.True(_handler.WasExecuted, $"Alias '{alias}' should trigger execution");
        }
    }

    [Fact] // проверяем подсказки для похожих команд
    public async Task Should_Throw_CommandSuggestions_When_Similar_Command_Found()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

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
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

        var context = CreateCommandContext("totally_unknown", ChatType.Private);

        await Assert.ThrowsAsync<RouteNotFoundException>(
            () => middleware.InvokeAsync(context, () => Task.CompletedTask));
    }

    [Fact] // команда не найдена в групповом чате
    public async Task Should_Ignore_Unknown_Command_In_Group_Chat()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

        var context = CreateCommandContext("totally_unknown", ChatType.Group);
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.False(_handler.WasExecuted);
        Assert.False(called); // ничего не вызывается, просто возвращаемся
    }

    [Fact] // команда предназначена для другого бота
    public async Task Should_Ignore_Command_For_Different_Bot()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

        var context = CreateCommandContext("bday_add", ChatType.Private, targetBotUsername: "OtherBot");
        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.False(_handler.WasExecuted);
        Assert.False(called);
    }

    [Fact] // виртуальный роут успешно находит обработчик команды
    public async Task Should_Execute_Command_When_VirtualRoute_Matches()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set"; // устанавливаем виртуальный маршрут

        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.True(_handler.WasExecuted);
        Assert.False(called);
    }

    [Fact] // виртуальный роут имеет приоритет над оригинальной командой
    public async Task Should_Prefer_VirtualRoute_Over_Original_Command()
    {
        var playHandler = new MockCommandHandler("play");
        var bdayHandler = new MockCommandHandler("bday_set");

        var playInvoker = new CommandHandlerInvoker<object>(playHandler, new MockCommandParameterBinder());
        var bdayInvoker = new CommandHandlerInvoker<object>(bdayHandler, new MockCommandParameterBinder());

        var middleware = new CommandRoutingMiddleware([playInvoker, bdayInvoker], _botInfo);

        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_set";

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        Assert.False(playHandler.WasExecuted, "play handler should not be executed");
        Assert.True(bdayHandler.WasExecuted, "bday_set handler should be executed via virtual route");
    }

    [Fact] // виртуальный роут не найден - выбрасываем VirtualRouteNotFoundException
    public async Task Should_Throw_VirtualRouteNotFoundException_When_VirtualRoute_Not_Found()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "non_existent_route";

        var exception = await Assert.ThrowsAsync<VirtualRouteNotFoundException>(
            () => middleware.InvokeAsync(context, () => Task.CompletedTask));

        Assert.Equal("play", exception.Command);
        Assert.Equal("non_existent_route", exception.VirtualRoute);
    }

    [Fact] // виртуальный роут работает с алиасами
    public async Task Should_Execute_Alias_When_VirtualRoute_Matches_Alias()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandRoutingMiddleware([invoker], _botInfo);

        var context = CreateCommandContext("play", ChatType.Private);
        context.VirtualRoute = "bday_create"; // виртуальный роут указывает на алиас

        await middleware.InvokeAsync(context, () => Task.CompletedTask);

        Assert.True(_handler.WasExecuted);
    }
}
