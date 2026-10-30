using FluentAssertions;
using Telegram.BotKit.Commands.Attributes;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Commands.CommandParameterBinderTests;

public sealed partial class CommandParameterBinderTests
{
    private record FlagCommandParams
    {
        [CommandParam(Name = "active")]
        public bool IsActive { get; init; }
    }

    // understands "yes" and "no" instead of "true" and "false"
    private sealed class YesNoConverter : IValueConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(bool);

        public object? Convert(ValueConversionContext context) =>
            TryConvert(context, out var result) ? result : throw new FormatException(context.Input);

        public bool TryConvert(ValueConversionContext context, out object? result)
        {
            result = context.Input.ToLowerInvariant() switch
            {
                "yes" => true,
                "no" => false,
                _ => (bool?)null
            };

            return result is not null;
        }
    }

    // verifies that a converter registered after the default one has the priority
    [Fact]
    public void Bind_WhenCustomConverterIsRegisteredAfterDefault_ShouldUseIt()
    {
        var binder = new CommandParameterBinder(new IValueConverter[] { new DefaultValueConverter(), new YesNoConverter() });

        var result = binder.Bind<FlagCommandParams>(CreateContext("/cmd active=yes"));

        result.IsActive.Should().BeTrue();
    }

    // verifies that a converter registered before the default one is shadowed by it
    [Fact]
    public void Bind_WhenCustomConverterIsRegisteredBeforeDefault_ShouldUseDefaultOne()
    {
        var binder = new CommandParameterBinder(new IValueConverter[] { new YesNoConverter(), new DefaultValueConverter() });

        var act = () => binder.Bind<FlagCommandParams>(CreateContext("/cmd active=yes"));

        act.Should().Throw<InvalidParameterTypeException>();
    }

    // verifies that the converter with the priority is the only one asked: a value it rejects is not passed on to the
    // others (a custom converter for a built-in type has to handle the usual formats as well)
    [Fact]
    public void Bind_WhenPriorityConverterRejectsValue_ShouldNotFallBackToOtherConverters()
    {
        var binder = new CommandParameterBinder(new IValueConverter[] { new DefaultValueConverter(), new YesNoConverter() });

        var act = () => binder.Bind<FlagCommandParams>(CreateContext("/cmd active=true"));

        act.Should().Throw<InvalidParameterTypeException>();
    }
}
