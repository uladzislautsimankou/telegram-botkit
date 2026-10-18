using System.Reflection;
using Telegram.BotKit.Callbacks.Attributes;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Binding;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Callbacks.Binding;

/// <summary>
/// Binds incoming callback parameters to strongly-typed models using compiled setters and value converters.
/// </summary>
/// <param name="converters">The registered value converters; the one registered last has the highest priority.</param>
internal sealed class CallbackParameterBinder(IEnumerable<IValueConverter> converters)
    : ParameterBinderBase(converters), ICallbackParameterBinder
{
    /// <inheritdoc />
    public TParams Bind<TParams>(CallbackContext context) where TParams : class, new()
    {
        var plan = GetPlan<TParams>();

        // models without parameters (e.g. EmptyCallbackParams) ignore the query string entirely;
        // an empty query string is still checked against [CallbackParam(Required = true)]
        return plan.Params.Length == 0
            ? new TParams()
            : BindCore<TParams>(plan, null, context.RawParams);
    }

    /// <inheritdoc />
    // callback data is "key?name=value": every public property with a getter and a setter is a named parameter,
    // [CallbackParam] only overrides its name and requiredness.
    // Keep in sync with CallbackDataBuilder, which writes exactly these parameters: it has to read the value,
    // so a property without a getter could be bound here but never produced there.
    protected override ParamMetadata? MapProperty(PropertyInfo property, Func<Type, IValueConverter> resolveConverter)
    {
        if (!property.CanRead)
        {
            return null;
        }

        var attribute = property.GetCustomAttribute<CallbackParamAttribute>();

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


