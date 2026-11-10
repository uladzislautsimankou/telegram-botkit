using FluentAssertions;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;
using Telegram.BotKit.WidgetQueries.Attributes;
using Telegram.BotKit.WidgetQueries.Binding;

namespace Telegram.BotKit.Tests.WidgetQueries.WidgetParameterBinderTests;

public sealed partial class WidgetParameterBinderTests
{
    private record FlagWidgetParams
    {
        [WidgetParam(Name = "a")]
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
        var binder = new WidgetParameterBinder(new IValueConverter[] { new DefaultValueConverter(), new YesNoConverter() });

        var result = binder.Bind<FlagWidgetParams>(CreateContext("test?a=yes"));

        result.IsActive.Should().BeTrue();
    }

    // verifies that the converter with the priority is the only one asked: a value it rejects is not passed on to the others
    [Fact]
    public void Bind_WhenPriorityConverterRejectsValue_ShouldNotFallBackToOtherConverters()
    {
        var binder = new WidgetParameterBinder(new IValueConverter[] { new DefaultValueConverter(), new YesNoConverter() });

        var act = () => binder.Bind<FlagWidgetParams>(CreateContext("test?a=true"));

        act.Should().Throw<InvalidParameterTypeException>();
    }
}
