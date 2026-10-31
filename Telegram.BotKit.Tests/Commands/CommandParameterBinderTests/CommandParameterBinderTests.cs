using Telegram.Bot.Types;
using Telegram.BotKit.Commands.Attributes;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Tests.Commands.CommandParameterBinderTests;

public sealed partial class CommandParameterBinderTests
{
    private readonly CommandParameterBinder _binder;

    public CommandParameterBinderTests()
    {
        var converters = new List<IValueConverter>
        {
            new DefaultValueConverter()
        };

        _binder = new CommandParameterBinder(converters);
    }

    private record ComplexCommandParams
    {
        [CommandParam(Position = 0)]
        public string Name { get; init; } = string.Empty;

        [CommandParam(Position = 1)]
        public int Age { get; init; }

        [CommandParam(Position = 2)]
        public TestEnum Status { get; init; }

        [CommandParam(Position = 3)]
        public DateTime Date { get; init; }

        [CommandParam(Name = "uid")]
        public Guid? UserId { get; init; }

        [CommandParam(Name = "active", Required = true)]
        public bool IsActive { get; init; }
    }

    private CommandContext CreateContext(string fullText)
    {
        var msg = new Message
        {
            Text = fullText,
            From = new User { Id = 1, FirstName = "User" },
            Chat = new Chat { Id = 1 }
        };
        return new CommandContext(msg);
    }

    private record SearchCommandParams
    {
        [CommandParam(Position = 0)]
        public string Query { get; init; } = string.Empty;
    }

    private record GappedCommandParams
    {
        [CommandParam(Position = 0)]
        public string First { get; init; } = string.Empty;

        [CommandParam(Position = 5)]
        public int Second { get; init; }
    }

    private record TwoStringsCommandParams
    {
        [CommandParam(Position = 0)]
        public string Verb { get; init; } = string.Empty;

        [CommandParam(Position = 1)]
        public string Text { get; init; } = string.Empty;
    }

    private record DuplicatePositionParams
    {
        [CommandParam(Position = 0)]
        public string A { get; init; } = string.Empty;

        [CommandParam(Position = 0)]
        public string B { get; init; } = string.Empty;
    }

    private record DuplicateNameParams
    {
        [CommandParam(Name = "item")]
        public string A { get; init; } = string.Empty;

        [CommandParam(Name = "ITEM")]
        public string B { get; init; } = string.Empty;
    }
}
