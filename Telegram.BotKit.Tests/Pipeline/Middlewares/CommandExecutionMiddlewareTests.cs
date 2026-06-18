using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Invocation;
using Telegram.BotKit.Pipeline.Middlewares;
using static Telegram.BotKit.Tests.Pipeline.Middlewares.CommandMiddlewaresTestsMocks;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public class CommandExecutionMiddlewareTests
{
    private readonly MockCommandHandler _handler = new("bday_set");

    [Fact] // успешный вызов хендлера, если в контексте лежит MatchedInvoker
    public async Task Should_Execute_Command_When_MatchedInvoker_Is_Set()
    {
        var binder = new MockCommandParameterBinder();
        var invoker = new CommandHandlerInvoker<object>(_handler, binder);
        var middleware = new CommandExecutionMiddleware();

        var context = CreateCommandContext("bday_set", ChatType.Private);
        context.MatchedInvoker = invoker;

        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.True(_handler.WasExecuted);
        Assert.False(called);
    }

    [Fact] // ничего не делает, если в контексте нет MatchedInvoker
    public async Task Should_Do_Nothing_When_MatchedInvoker_Is_Null()
    {
        var middleware = new CommandExecutionMiddleware();
        var context = CreateCommandContext("bday_set", ChatType.Private);

        var called = false;
        NextDelegate nextDelegate = async () => { called = true; await Task.CompletedTask; };

        await middleware.InvokeAsync(context, nextDelegate);

        Assert.False(_handler.WasExecuted);
        Assert.False(called);
    }

    private CommandContext CreateCommandContext(string command, ChatType chatType)
    {
        var message = new Telegram.Bot.Types.Message
        {
            Chat = new Telegram.Bot.Types.Chat { Id = 123, Type = chatType },
            From = new Telegram.Bot.Types.User { Id = 456, FirstName = "Test" },
            Text = $"/{command}"
        };

        return new CommandContext(message);
    }
}