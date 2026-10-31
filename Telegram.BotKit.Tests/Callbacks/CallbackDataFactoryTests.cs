using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Callbacks.Building;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Callbacks;

public sealed class CallbackDataFactoryTests
{
    private sealed record Money(decimal Amount, string Currency);

    private sealed class PriceParams
    {
        public Money? Price { get; set; }

        public int Count { get; set; }

        public bool Flag { get; set; }
    }

    private sealed class PriceHandler : ICallbackHandler<PriceParams>
    {
        public static string Key => "shop:price";

        public Task HandleAsync(PriceParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // a type with its own text form: the default formatting (ToString) could not be read back
    private sealed class MoneyConverter : IValueConverter
    {
        public string Separator { get; init; } = ":";

        public bool CanConvert(Type targetType) => targetType == typeof(Money);

        public object? Convert(ValueConversionContext context) =>
            TryConvert(context, out var result) ? result : throw new FormatException($"'{context.Input}' is not money.");

        public bool TryConvert(ValueConversionContext context, out object? result)
        {
            var parts = context.Input.Split(Separator);
            if (parts.Length == 2 && decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                result = new Money(amount, parts[1]);
                return true;
            }

            result = null;
            return false;
        }

        public bool TryFormat(object value, [NotNullWhen(true)] out string? text)
        {
            if (value is Money money)
            {
                text = money.Amount.ToString("0.00", CultureInfo.InvariantCulture) + Separator + money.Currency;
                return true;
            }

            text = null;
            return false;
        }
    }

    // overrides how a built-in type is written: "yes"/"no" instead of "true"/"false"
    private sealed class YesNoConverter : IValueConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(bool);

        public object? Convert(ValueConversionContext context) => context.Input == "yes";

        public bool TryConvert(ValueConversionContext context, out object? result)
        {
            result = context.Input == "yes";
            return context.Input is "yes" or "no";
        }

        public bool TryFormat(object value, [NotNullWhen(true)] out string? text)
        {
            text = (bool)value ? "yes" : "no";
            return true;
        }
    }

    // converts ints but does not format them: relies on the default TryFormat of the interface
    private sealed class ParseOnlyIntConverter : IValueConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(int);

        public object? Convert(ValueConversionContext context) => int.Parse(context.Input, CultureInfo.InvariantCulture);

        public bool TryConvert(ValueConversionContext context, out object? result)
        {
            var ok = int.TryParse(context.Input, CultureInfo.InvariantCulture, out var value);
            result = value;
            return ok;
        }
    }

    // claims to format everything, but does not support any type: must never be asked
    private sealed class UnsupportedTypesConverter : IValueConverter
    {
        public bool CanConvert(Type targetType) => false;

        public object? Convert(ValueConversionContext context) => throw new NotSupportedException();

        public bool TryConvert(ValueConversionContext context, out object? result)
        {
            result = null;
            return false;
        }

        public bool TryFormat(object value, [NotNullWhen(true)] out string? text)
        {
            text = "WRONG";
            return true;
        }
    }

    private static CallbackDataFactory CreateFactory(params IValueConverter[] converters) =>
        new([new DefaultValueConverter(), .. converters]);

    private static PriceParams BindBack(string data, params IValueConverter[] converters) =>
        new CallbackParameterBinder([new DefaultValueConverter(), .. converters])
            .Bind<PriceParams>(new CallbackContext(new CallbackQuery { Id = "1", Data = data }));

    // verifies that a type with its own converter is written by it and read back unchanged by the binder
    [Fact]
    public void Create_WhenConverterFormatsType_ShouldRoundTripThroughBinder()
    {
        var converter = new MoneyConverter();
        var factory = CreateFactory(converter);

        var data = factory.Create<PriceHandler, PriceParams>(new PriceParams { Price = new Money(12.5m, "USD"), Count = 2 });
        var result = BindBack(data, converter);

        data.Should().Be("shop:price?Count=2&Price=12.50:USD");
        result.Price.Should().Be(new Money(12.5m, "USD"));
        result.Count.Should().Be(2);
    }

    // verifies that the static API, which has no access to the registered converters, falls back to ToString
    [Fact]
    public void StaticBuilder_WhenTypeHasCustomConverter_ShouldNotUseIt()
    {
        var data = CallbackDataBuilder.Build("shop:price", new PriceParams { Price = new Money(12.5m, "USD") });

        data.Should().NotContain("12.50:USD").And.Contain("Money");
    }

    // verifies that the converter registered last wins, as it does when the data is read
    [Fact]
    public void Create_WhenSeveralConvertersFormatTheType_ShouldUseTheOneRegisteredLast()
    {
        var factory = CreateFactory(new MoneyConverter { Separator = ":" }, new MoneyConverter { Separator = "/" });

        var data = factory.Create("shop:price", new PriceParams { Price = new Money(3m, "EUR") });

        data.Should().Be("shop:price?Price=3.00/EUR");
    }

    // verifies that a converter can override the way a built-in type is written
    [Fact]
    public void Create_WhenConverterFormatsBuiltInType_ShouldOverrideBuiltInFormatting()
    {
        var converter = new YesNoConverter();
        var factory = CreateFactory(converter);

        var data = factory.Create("shop:price", new PriceParams { Flag = true });
        var result = BindBack(data, converter);

        data.Should().Be("shop:price?Flag=yes");
        result.Flag.Should().BeTrue();
    }

    // verifies that a converter without formatting (the interface default) leaves the value to the built-in formatting
    [Fact]
    public void Create_WhenConverterDoesNotFormat_ShouldUseBuiltInFormatting()
    {
        var factory = CreateFactory(new ParseOnlyIntConverter());

        var data = factory.Create("shop:price", new PriceParams { Count = 5 });

        data.Should().Be("shop:price?Count=5");
    }

    // verifies that a converter is only asked about the types it supports
    [Fact]
    public void Create_WhenConverterDoesNotSupportTheType_ShouldNotAskItToFormat()
    {
        var factory = CreateFactory(new UnsupportedTypesConverter());

        var data = factory.Create("shop:price", new PriceParams { Count = 5 });

        data.Should().Be("shop:price?Count=5");
    }

    // verifies that text produced by a converter is escaped like any other value
    [Fact]
    public void Create_WhenFormattedTextHasStructuralCharacters_ShouldEscapeThem()
    {
        var converter = new MoneyConverter();
        var factory = CreateFactory(converter);

        var data = factory.Create("shop:price", new PriceParams { Price = new Money(1m, "A&B=C") });
        var result = BindBack(data, converter);

        data.Should().Contain("A%26B%3DC");
        result.Price.Should().Be(new Money(1m, "A&B=C"));
    }

    // verifies that Telegram's length limit applies to the data built by the factory too
    [Fact]
    public void Create_WhenDataIsTooLong_ShouldThrowCallbackDataTooLongException()
    {
        var factory = CreateFactory(new MoneyConverter());

        var act = () => factory.Create<PriceHandler, PriceParams>(new PriceParams { Price = new Money(1m, new string('x', 80)) });

        act.Should().Throw<CallbackDataTooLongException>();
    }

    // verifies that a handler without parameters is created by its key
    [Fact]
    public void Create_ForHandlerWithoutParameters_ShouldReturnKey()
    {
        var factory = CreateFactory();

        factory.Create<NoParamsHandler>().Should().Be("menu");
    }

    private sealed class NoParamsHandler : ICallbackHandler<EmptyCallbackParams>
    {
        public static string Key => "menu";

        public Task HandleAsync(EmptyCallbackParams parameters, CallbackContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    // verifies that the converters registered in the container reach the factory
    [Fact]
    public void AddCallbacks_ShouldRegisterFactoryThatUsesRegisteredConverters()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValueConverter, DefaultValueConverter>();
        services.AddSingleton<IValueConverter>(new MoneyConverter());
        services.AddCallbacks(null, typeof(IUpdateHandler).Assembly);
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<ICallbackDataFactory>();
        var data = factory.Create("shop:price", new PriceParams { Price = new Money(7m, "PLN") });

        data.Should().Be("shop:price?Price=7.00:PLN");
    }

    // verifies that the default converter does not claim to format anything, leaving that to the built-in formatting
    [Fact]
    public void TryFormat_OnDefaultConverter_ShouldNotFormat()
    {
        IValueConverter converter = new DefaultValueConverter();

        converter.TryFormat(5, out var text).Should().BeFalse();
        text.Should().BeNull();
    }
}
