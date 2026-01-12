using Telegram.BotKit.Binding.Converters;

namespace Telegram.BotKit.Tests.Binding.Converters;

public partial class ValueConverterTests
{
    private class NoConverterType { }

    [Fact]
    public void Convert_ShouldReturnNull_ForEmptyString()
    {
        var result = ValueConverter.Convert("", typeof(int?));
        Assert.Null(result);
    }
}
