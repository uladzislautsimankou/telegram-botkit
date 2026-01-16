using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Attributes;
using Telegram.BotKit.Binding.Binders;
using Telegram.BotKit.Binding.Converters;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Tests.Shared;

namespace Telegram.BotKit.Tests.Binding.Binders;

public class CommandParameterBinderTests
{
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

        [CommandParam(Name = "uid")] // только именованный alias
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

    private readonly CommandParameterBinder _binder;

    public CommandParameterBinderTests()
    {
        var converters = new List<IValueConverter>
        {
            new DefaultValueConverter()
        };

        _binder = new CommandParameterBinder(converters);
    }

    #region Позиции и имена
    [Fact]
    public void Bind_ShouldHandleAllPositional()
    {
        // передаем всё позиционно (кроме чисто именованных)
        // /cmd [Name] [Age] [Status] [Date] active=true
        var ctx = CreateContext("/cmd SomeName 25 First 2025-01-20 active=true");

        var result = _binder.Bind<ComplexCommandParams>(ctx);

        Assert.Equal("SomeName", result.Name);
        Assert.Equal(25, result.Age);
        Assert.Equal(TestEnum.First, result.Status);
        Assert.Equal(new DateTime(2025, 01, 20), result.Date);
        Assert.True(result.IsActive);
    }

    [Fact]
    public void Bind_ShouldHandleAllNamed()
    {
        // хаотичный порядок, биндим только по именам
        var guid = Guid.NewGuid();
        var ctx = CreateContext($"/cmd active=true Name=SomeName Date=2025-12-31 uid={guid} Age=30 Status=Second");

        var result = _binder.Bind<ComplexCommandParams>(ctx);

        Assert.Equal("SomeName", result.Name);
        Assert.Equal(30, result.Age);
        Assert.Equal(TestEnum.Second, result.Status);
        Assert.Equal(new DateTime(2025, 12, 31), result.Date);
        Assert.Equal(guid, result.UserId);
        Assert.True(result.IsActive);
    }

    [Fact]
    public void Bind_ShouldHandleHybrid_SkipMiddlePositional()
    {
        // смешаный порядок:
        // [Name] - позиция (из очереди)
        // [Age] - по имени (очередь пропускаем)
        // [Status] - позиция (из очереди)
        // [Date] - позиция (из очереди)
        // [IsActive] - по имени
        var ctx = CreateContext("/cmd SomeName Age=99 Second 2022-02-02 active=true");

        var result = _binder.Bind<ComplexCommandParams>(ctx);

        Assert.Equal("SomeName", result.Name);
        Assert.Equal(99, result.Age);
        Assert.Equal(TestEnum.Second, result.Status);
        Assert.Equal(new DateTime(2022, 02, 02), result.Date);
    }
    #endregion

    #region Типы
    [Fact]
    public void Bind_ShouldMapComplexTypes()
    {
        // проверяем Guid и Date в разных форматах
        var guid = Guid.NewGuid();
        var ctx = CreateContext($"/cmd SomeName 1 First 31.12.2025 active=false uid={guid}");

        var result = _binder.Bind<ComplexCommandParams>(ctx);

        Assert.Equal(new DateTime(2025, 12, 31), result.Date);
        Assert.Equal(guid, result.UserId);
        Assert.False(result.IsActive);
    }

    [Fact]
    public void Bind_ShouldMapEnum_AsInt()
    {
        // проверяем enum Status=1 (First)
        var ctx = CreateContext("/cmd SomeName 1 1 2025-01-01 active=true");
        var result = _binder.Bind<ComplexCommandParams>(ctx);
        Assert.Equal(TestEnum.First, result.Status);
    }

    [Fact]
    public void Bind_ShouldHandleQuotedString_Positional()
    {
        // проверяем, что кавычки удаляются парсером, а пробел внутри сохраняется
        var ctx = CreateContext("/cmd \"John Doe\" 25 First 2025-01-01 active=true");

        var result = _binder.Bind<ComplexCommandParams>(ctx);

        Assert.Equal("John Doe", result.Name);
        Assert.Equal(25, result.Age);
    }

    [Fact]
    public void Bind_ShouldHandleQuotedString_Named()
    {
        // проверяем, что из именованных параметров с кавычками нормально вынимается текст
        var ctx = CreateContext("/cmd Name=\"John Doe\" 25 First 2025-01-01 active=true");

        var result = _binder.Bind<ComplexCommandParams>(ctx);

        Assert.Equal("John Doe", result.Name);
        Assert.Equal(25, result.Age);
    }
    #endregion

    #region Валидация
    [Fact]
    public void Bind_ShouldThrow_WhenMissingRequired()
    {
        // нет required (IsActive)
        var ctx = CreateContext("/cmd SomeName 25 First 2025-01-01");

        var ex = Assert.Throws<MissingParameterException>(() =>
            _binder.Bind<ComplexCommandParams>(ctx));

        Assert.Equal("active", ex.ParameterName);
    }

    [Fact]
    public void Bind_ShouldThrow_WhenTypeMismatch_Enum()
    {
        // несуществующий enum (Status = 99)
        var ctx = CreateContext("/cmd SomeName 25 99 2025-01-01 active=true");

        var ex = Assert.Throws<InvalidParameterTypeException>(() =>
            _binder.Bind<ComplexCommandParams>(ctx));

        Assert.Equal("Status", ex.ParameterName);
    }

    [Fact]
    public void Bind_ShouldThrow_WhenTypeMismatch_Int()
    {
        // Age="old"
        var ctx = CreateContext("/cmd SomeName old First 2025-01-01 active=true");

        var ex = Assert.Throws<InvalidParameterTypeException>(() =>
            _binder.Bind<ComplexCommandParams>(ctx));

        Assert.Equal("Age", ex.ParameterName);
        Assert.Equal("old", ex.RawValue);
    }
    #endregion
}