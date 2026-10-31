using FluentAssertions;
using Telegram.BotKit.Callbacks.Attributes;
using Telegram.BotKit.Callbacks.Binding;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Tests.Callbacks.CallbackParameterBinderTests;

public sealed partial class CallbackParameterBinderTests
{
    private record FlagCallbackParams
    {
        [CallbackParam(Name = "a")]
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
        var binder = new CallbackParameterBinder(new IValueConverter[] { new DefaultValueConverter(), new YesNoConverter() });

        var result = binder.Bind<FlagCallbackParams>(CreateContext("test?a=yes"));

        result.IsActive.Should().BeTrue();
    }

    // verifies that the converter with the priority is the only one asked: a value it rejects is not passed on to the others
    [Fact]
    public void Bind_WhenPriorityConverterRejectsValue_ShouldNotFallBackToOtherConverters()
    {
        var binder = new CallbackParameterBinder(new IValueConverter[] { new DefaultValueConverter(), new YesNoConverter() });

        var act = () => binder.Bind<FlagCallbackParams>(CreateContext("test?a=true"));

        act.Should().Throw<InvalidParameterTypeException>();
    }
}
