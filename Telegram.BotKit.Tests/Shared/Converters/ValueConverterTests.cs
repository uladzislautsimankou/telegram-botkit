using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Tests.Shared.Converters;

public sealed partial class ValueConverterTests
{
    private class NoConverterType { }

    private readonly IValueConverter _converter = new DefaultValueConverter();
}