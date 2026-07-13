using System.Reflection;
using Telegram.BotKit.Commands.Attributes;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Binding;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Commands.Binding;

/// <summary>
/// Command parameter binder responsible for mapping incoming command raw arguments to strongly-typed models.
/// </summary>
internal sealed class CommandParameterBinder(
    IEnumerable<IValueConverter> converters
    ) : ParameterBinderBase<CommandParamAttribute> (converters), ICommandParameterBinder
{
    public TParams Bind<TParams>(CommandContext context) where TParams : class, new() =>
         BindInternal<TParams>(context.RawParams);

    protected override ParamMetadata MapAttribute(PropertyInfo prop, CommandParamAttribute attr) => 
        new ParamMetadata(
            PropertyInfo: prop,
            Name: attr.Name,
            Position: attr.Position,
            Required: attr.Required,
            PropertyType: prop.PropertyType
        );

}