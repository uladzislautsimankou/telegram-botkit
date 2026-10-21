using FluentAssertions;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Attributes;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Building;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackDataTests
{
    private readonly CallbackParameterBinder _binder = new([new DefaultValueConverter()]);

    private sealed class RoundTripParams
    {
        public int Number { get; set; } = 7;
        public long Big { get; set; }
        public double Ratio { get; set; }
        public decimal Money { get; set; }
        public bool Flag { get; set; }
        public string? Text { get; set; }
        public TestEnum Kind { get; set; }
        public Guid Id { get; set; }
        public DateTime When { get; set; }
        public DateOnly Day { get; set; }
        public TimeOnly Time { get; set; }
        public TimeSpan Span { get; set; }
        public int? Optional { get; set; }
    }

    private sealed class MoveParams
    {
        public int Y { get; set; }
        public int X { get; set; }
    }

    private sealed class TextParams
    {
        public string? Text { get; set; }
    }

    private sealed class RequiredParams
    {
        [CallbackParam(Required = true)]
        public int Id { get; set; }
    }

    private sealed class RequiredTextParams
    {
        [CallbackParam(Required = true)]
        public string? Name { get; set; }
    }

    private sealed class RenamedParams
    {
        [CallbackParam(Name = "i")]
        public int ItemId { get; set; }
    }

    private sealed class DuplicateNameParams
    {
        [CallbackParam(Name = "x")]
        public int A { get; set; }

        [CallbackParam(Name = "X")]
        public int B { get; set; }
    }

    private sealed class MoveHandler : ICallbackHandler<MoveParams>
    {
        public static string Key => "game:move";

        public Task HandleAsync(MoveParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MenuHandler : ICallbackHandler<EmptyCallbackParams>
    {
        public static string Key => "menu";

        public Task HandleAsync(EmptyCallbackParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private TParams RoundTrip<TParams>(string data) where TParams : class, new() =>
        _binder.Bind<TParams>(new CallbackContext(new CallbackQuery { Id = "1", Data = data }));

    // verifies that every supported value type survives build -> parse -> bind unchanged
    [Fact]
    public void Build_WhenAllValueTypesAreSet_ShouldRoundTripThroughBinder()
    {
        // arrange
        var original = new RoundTripParams
        {
            Number = 42,
            Big = 9_000_000_000,
            Ratio = 0.1,
            Money = 12.5m,
            Flag = true,
            Text = "a b&c=d%e?f:g",
            Kind = TestEnum.Second,
            Id = Guid.NewGuid(),
            When = new DateTime(2025, 12, 31, 14, 30, 15),
            Day = new DateOnly(2025, 1, 2),
            Time = new TimeOnly(9, 5, 7),
            Span = TimeSpan.FromMinutes(90),
            Optional = 5
        };

        // act: the unlimited internal builder, the model is deliberately larger than 64 bytes
        var data = CallbackDataBuilder.Build("test", original);
        var result = RoundTrip<RoundTripParams>(data);

        // assert
        result.Should().BeEquivalentTo(original);
    }

    // verifies that a model equal to its defaults produces just the key and binds back to the defaults
    [Fact]
    public void Build_WhenAllValuesAreDefault_ShouldProduceKeyOnly()
    {
        var data = CallbackDataBuilder.Build("test", new RoundTripParams());

        data.Should().Be("test");
        RoundTrip<RoundTripParams>(data).Should().BeEquivalentTo(new RoundTripParams());
    }

    // verifies that a value equal to the CLR default but different from the model default is not omitted
    [Fact]
    public void Build_WhenValueDiffersFromModelDefaultButEqualsClrDefault_ShouldWriteIt()
    {
        var data = CallbackDataBuilder.Build("test", new RoundTripParams { Number = 0 });

        data.Should().Be("test?Number=0");
        RoundTrip<RoundTripParams>(data).Number.Should().Be(0);
    }

    // verifies that required parameters are written even when they equal the default
    [Fact]
    public void Build_WhenRequiredParameterHasDefaultValue_ShouldStillWriteIt()
    {
        var data = CallbackDataBuilder.Build("test", new RequiredParams());

        data.Should().Be("test?Id=0");
        RoundTrip<RequiredParams>(data).Id.Should().Be(0);
    }

    // verifies that a required parameter without a value fails where the button is built, not when it is clicked
    [Fact]
    public void Build_WhenRequiredParameterIsNull_ShouldThrowArgumentException()
    {
        var act = () => CallbackDataBuilder.Build("test", new RequiredTextParams());

        act.Should().Throw<ArgumentException>()
            .WithParameterName("parameters")
            .WithMessage("*'Name'*'RequiredTextParams'*");
    }

    // verifies that the public entry point rejects a required parameter without a value as well
    [Fact]
    public void Create_WhenRequiredParameterIsNull_ShouldThrowArgumentException()
    {
        var act = () => CallbackData.Create("test", new RequiredTextParams());

        act.Should().Throw<ArgumentException>();
    }

    // verifies that a required parameter with a value is written and read back
    [Fact]
    public void Build_WhenRequiredParameterHasValue_ShouldWriteIt()
    {
        var data = CallbackDataBuilder.Build("test", new RequiredTextParams { Name = "x" });

        data.Should().Be("test?Name=x");
        RoundTrip<RequiredTextParams>(data).Name.Should().Be("x");
    }

    // verifies that [CallbackParam] names are used and that parameters are ordered by name
    [Fact]
    public void Build_WhenNamesAreCustomized_ShouldUseThemAndOrderParametersByName()
    {
        CallbackDataBuilder.Build("k", new RenamedParams { ItemId = 5 }).Should().Be("k?i=5");
        CallbackDataBuilder.Build("k", new MoveParams { Y = 2, X = 1 }).Should().Be("k?X=1&Y=2");
    }

    // verifies that structural characters are escaped and nothing else
    [Fact]
    public void Build_WhenValueContainsStructuralCharacters_ShouldEscapeOnlyThem()
    {
        var data = CallbackDataBuilder.Build("k", new TextParams { Text = "a&b=c%d e:f" });

        data.Should().Be("k?Text=a%26b%3Dc%25d e:f");
        RoundTrip<TextParams>(data).Text.Should().Be("a&b=c%d e:f");
    }

    // verifies that null values are omitted
    [Fact]
    public void Build_WhenValueIsNull_ShouldOmitParameter() =>
        CallbackDataBuilder.Build("k", new TextParams { Text = null }).Should().Be("k");

    // verifies that invalid keys are rejected
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("game?move")]
    public void Create_WhenKeyIsInvalid_ShouldThrowArgumentException(string key)
    {
        var act = () => CallbackData.Create(key, new MoveParams());

        act.Should().Throw<ArgumentException>();
    }

    // verifies that duplicate parameter names in the model are rejected
    [Fact]
    public void Create_WhenModelDeclaresDuplicateNames_ShouldThrowInvalidOperationException()
    {
        var act = () => CallbackData.Create("k", new DuplicateNameParams { A = 1 });

        act.Should().Throw<InvalidOperationException>().WithMessage("*more than once*");
    }

    // verifies the 64 byte limit boundary
    [Fact]
    public void Create_WhenDataIsExactlyAtLimit_ShouldSucceed()
    {
        // "k?Text=" is 7 bytes
        var data = CallbackData.Create("k", new TextParams { Text = new string('a', 57) });

        data.Should().HaveLength(64);
    }

    // verifies that the limit is measured in UTF-8 bytes, not characters
    [Fact]
    public void Create_WhenDataFitsInCharactersButNotInBytes_ShouldThrowCallbackDataTooLongException()
    {
        // 7 + 30 characters, but 7 + 60 bytes
        var act = () => CallbackData.Create("k", new TextParams { Text = new string('я', 30) });

        act.Should().Throw<CallbackDataTooLongException>()
            .Which.Should().Match<CallbackDataTooLongException>(e => e.ByteCount == 67 && e.MaxBytes == 64);
    }

    // verifies that the handler's route key is used
    [Fact]
    public void Create_WhenHandlerIsSpecified_ShouldUseHandlerKey()
    {
        CallbackData.Create<MoveHandler, MoveParams>(new MoveParams { X = 1, Y = 2 }).Should().Be("game:move?X=1&Y=2");
        CallbackData.Create<MenuHandler>().Should().Be("menu");
    }

    // verifies that buttons carry the text and the handler-bound callback data
    [Fact]
    public void Button_WhenCreated_ShouldCarryTextAndCallbackData()
    {
        var button = CallbackButton.Create<MoveHandler, MoveParams>("Go", new MoveParams { X = 1, Y = 2 });

        button.Text.Should().Be("Go");
        button.CallbackData.Should().Be("game:move?X=1&Y=2");
        CallbackButton.Create<MenuHandler>("Menu").CallbackData.Should().Be("menu");
    }
}
