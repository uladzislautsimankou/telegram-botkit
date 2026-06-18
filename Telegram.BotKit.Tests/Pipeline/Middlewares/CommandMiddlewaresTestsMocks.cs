using Microsoft.Extensions.Options;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;

namespace Telegram.BotKit.Tests.Pipeline.Middlewares;

internal class CommandMiddlewaresTestsMocks
{
    internal class MockOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable OnChange(Action<T, string?> listener) => new DummyDisposable();

        private class DummyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    internal class MockBotInfo(string username) : IBotInfo
    {
        public string Username { get; } = username;

        public long Id { get; } = 123456789;

        public string FirstName { get; } = "Test";

        public string? LastName { get; } = "Test";
    }

    internal class MockCommandHandler(string command, string[]? aliases = null) : ICommandHandler<object>
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

    internal class MockCommandParameterBinder : ICommandParameterBinder
    {
        public TParams Bind<TParams>(CommandContext context) where TParams : class, new() => new();
    }
}
