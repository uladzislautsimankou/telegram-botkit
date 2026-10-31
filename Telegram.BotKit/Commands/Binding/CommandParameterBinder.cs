using System.Reflection;
using Telegram.BotKit.Commands.Attributes;
using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Binding;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Commands.Binding;

/// <summary>
/// Command parameter binder responsible for mapping incoming command raw arguments to strongly-typed models.
/// </summary>
/// <param name="converters">The registered value converters; the one registered last has the highest priority.</param>
internal sealed class CommandParameterBinder(IEnumerable<IValueConverter> converters)
    : ParameterBinderBase(converters), ICommandParameterBinder
{
    /// <inheritdoc />
    public TParams Bind<TParams>(CommandContext context) where TParams : class, new()
    {
        var plan = GetPlan<TParams>();

        // handlers without bindable parameters (e.g. EmptyCommandParams) must not trigger argument parsing
        if (plan.Params.Length == 0)
        {
            return new TParams();
        }

        var (positional, named) = SplitParams(context.RawParams, plan.NamedKeys);
        return BindCore<TParams>(plan, positional, named);
    }

    /// <inheritdoc />
    // commands bind only explicitly annotated properties: positions have to be declared
    protected override ParamMetadata? MapProperty(PropertyInfo property, Func<Type, IValueConverter> resolveConverter)
    {
        var attribute = property.GetCustomAttribute<CommandParamAttribute>();
        if (attribute is null)
        {
            return null;
        }

        return new ParamMetadata(
            PropertyInfo: property,
            Name: attribute.Name,
            Position: attribute.Position,
            Required: attribute.Required,
            PropertyType: property.PropertyType,
            Converter: resolveConverter(property.PropertyType),
            Setter: PropertySetterCompiler.Compile(property));
    }
}
