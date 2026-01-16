using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Converters;

namespace Telegram.BotKit.Tests.Binding.Converters;

public partial class ValueConverterTests
{
    private class NoConverterType { }

    private readonly IValueConverter _converter = new DefaultValueConverter();
}
