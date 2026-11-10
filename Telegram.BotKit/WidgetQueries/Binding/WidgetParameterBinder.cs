using System.Reflection;
using Telegram.BotKit.Shared.Binding;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.WidgetQueries.Attributes;
using Telegram.BotKit.WidgetQueries.Models;

namespace Telegram.BotKit.WidgetQueries.Binding;

/// <summary>
/// Binds incoming widget query parameters to strongly-typed models using compiled setters and value converters.
/// </summary>
/// <param name="converters">The registered value converters; the one registered last has the highest priority.</param>
internal sealed class WidgetParameterBinder(IEnumerable<IValueConverter> converters)
    : ParameterBinderBase(converters), IWidgetParameterBinder
{
    /// <inheritdoc />
    public TParams Bind<TParams>(WidgetQueryContext context) where TParams : class, new()
    {
        var plan = GetPlan<TParams>();

        // models without parameters (e.g. EmptyWidgetQueryParams) ignore the query string entirely;
        // an empty query string is still checked against [WidgetParam(Required = true)]
        return plan.Params.Length == 0
            ? new TParams()
            : BindCore<TParams>(plan, null, context.RawParams);
    }

    /// <inheritdoc />
    // the data is "key?name=value": every public property with a setter is a named parameter,
    // [WidgetParam] only overrides its name and requiredness.
    // A property without a getter is not a parameter, as in the callback binder: a data builder for widgets
    // (the counterpart of CallbackDataBuilder) has to read the value, so it could never produce such a parameter.
    protected override ParamMetadata? MapProperty(PropertyInfo property, Func<Type, IValueConverter> resolveConverter)
    {
        if (!property.CanRead)
        {
            return null;
        }

        var attribute = property.GetCustomAttribute<WidgetParamAttribute>();

        return new ParamMetadata(
            PropertyInfo: property,
            Name: attribute?.Name,
            Position: -1,
            Required: attribute?.Required ?? false,
            PropertyType: property.PropertyType,
            Converter: resolveConverter(property.PropertyType),
            Setter: PropertySetterCompiler.Compile(property));
    }
}
