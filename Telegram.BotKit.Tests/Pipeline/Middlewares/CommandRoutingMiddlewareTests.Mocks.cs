using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

public partial class CommandRoutingMiddlewareTests
{
    private class MockBotInfo(string username) : IBotInfo
    {
        public string Username { get; } = username;

        public long Id { get; } = 123456789;
    }

    private class MockCommandHandler(string command, string[]? aliases = null) : ICommandHandler<object>
    {
        public string Command { get; } = command;

        public string[] Aliases { get; } = aliases ?? Array.Empty<string>();

        public bool WasExecuted { get; private set; }

        public void ResetExecution() => WasExecuted = false;

        public Task HandleAsync(object parameters, CommandContext context, CancellationToken cancellationToken = default)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }
    }

    private class MockCommandParameterBinder : ICommandParameterBinder
    {
        public TParams Bind<TParams>(CommandContext context) where TParams : class, new() => new();
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
