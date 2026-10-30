using System.Globalization;
using FluentAssertions;

namespace Telegram.BotKit.Tests.Shared.Converters;

public sealed partial class ValueConverterTests
{
    private static void WithCulture(string name, Action action)
    {
        var original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(name);
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // verifies that numbers are read the same way on servers whose culture uses a decimal comma
    [Theory]
    [InlineData("ru-RU")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("tr-TR")]
    public void TryConvert_WhenCurrentCultureIsNotInvariant_ShouldParseNumbersTheSameWay(string culture)
    {
        WithCulture(culture, () =>
        {
            foreach (var input in new[] { "12.5", "12,5" })
            {
                _converter.TryConvert(input, typeof(double), out var asDouble).Should().BeTrue();
                asDouble.Should().Be(12.5);

                _converter.TryConvert(input, typeof(decimal), out var asDecimal).Should().BeTrue();
                asDecimal.Should().Be(12.5m);

                _converter.TryConvert(input, typeof(double?), out var asNullableDouble).Should().BeTrue();
                asNullableDouble.Should().Be(12.5);
            }

            _converter.TryConvert("1234", typeof(int), out var asInt).Should().BeTrue();
            asInt.Should().Be(1234);
        });
    }

    // verifies that dates and times are read the same way on servers with another date format
    [Theory]
    [InlineData("ru-RU")]
    [InlineData("de-DE")]
    [InlineData("en-GB")]
    [InlineData("tr-TR")]
    public void TryConvert_WhenCurrentCultureIsNotInvariant_ShouldParseDatesAndTimesTheSameWay(string culture)
    {
        WithCulture(culture, () =>
        {
            foreach (var input in new[] { "12/31/2025", "31.12.2025", "2025-12-31" })
            {
                _converter.TryConvert(input, typeof(DateTime), out var result).Should().BeTrue();
                result.Should().Be(new DateTime(2025, 12, 31));
            }

            _converter.TryConvert("18:30", typeof(TimeOnly), out var time).Should().BeTrue();
            time.Should().Be(new TimeOnly(18, 30));
        });
    }
}
