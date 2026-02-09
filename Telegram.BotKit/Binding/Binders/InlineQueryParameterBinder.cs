using System.Reflection;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Attributes;

namespace Telegram.BotKit.Binding.Binders;

internal sealed class InlineQueryParameterBinder(
    IEnumerable<IValueConverter> converters
    ) : ParameterBinderBase<InlineQyeryParamAttribute>(converters), IInlineQueryParameterBinder
{
    public TParams Bind<TParams>(InlineQueryContext context) where TParams : class, new() => 
        BindInternal<TParams>(context.RawParams);

    protected override ParamMetadata MapAttribute(PropertyInfo prop, InlineQyeryParamAttribute attr) => 
        new ParamMetadata(
            PropertyInfo: prop,
            Name: attr.Name,
            Position: attr.Position,
            Required: attr.Required,
            PropertyType: prop.PropertyType
        );
}
