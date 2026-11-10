using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.WidgetQueries.Attributes;
using Telegram.BotKit.WidgetQueries.Binding;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.Tests.WidgetQueries.WidgetParameterBinderTests;

public sealed partial class WidgetParameterBinderTests
{
    public enum WidgetTestEnum
    {
        None = 0,
        First = 1,
        Second = 2
    }

    private record TestWidgetParams
    {
        public int Id { get; init; }

        [WidgetParam(Name = "s")]
        public string? Status { get; init; }

        public WidgetTestEnum Mode { get; init; }

        [WidgetParam(Name = "a")]
        public bool? IsActive { get; init; }

        public Guid? Uid { get; init; }

        public DateTime? Date { get; init; }
    }

    private record RequiredWidgetParams
    {
        [WidgetParam(Required = true)]
        public int Id { get; init; }
    }

    private record DuplicateNameWidgetParams
    {
        [WidgetParam(Name = "x")]
        public int A { get; init; }

        [WidgetParam(Name = "X")]
        public int B { get; init; }
    }

    private readonly WidgetParameterBinder _binder;

    public WidgetParameterBinderTests()
    {
        var converters = new List<IValueConverter>
        {
            new DefaultValueConverter()
        };

        _binder = new WidgetParameterBinder(converters);
    }

    private static WidgetQueryContext CreateContext(string data) => WidgetQueryTestData.Context(data);
}
