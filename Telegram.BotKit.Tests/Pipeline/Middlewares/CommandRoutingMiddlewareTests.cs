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
}
