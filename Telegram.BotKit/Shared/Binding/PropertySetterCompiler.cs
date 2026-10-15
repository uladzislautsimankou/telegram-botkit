using System.Linq.Expressions;
using System.Reflection;

namespace Telegram.BotKit.Shared.Binding;

/// <summary>
/// Compiles strongly typed property setter expressions into high-performance delegates.
/// </summary>
internal static class PropertySetterCompiler
{
    /// <summary>
    /// Compiles a property setter delegate using expression trees to avoid reflection overhead and boxing.
    /// </summary>
    /// <param name="property">The property to compile a setter for.</param>
    /// <returns>A compiled action taking target instance and property value.</returns>
    public static Action<object, object?> Compile(PropertyInfo property)
    {
        var targetParam = Expression.Parameter(typeof(object), "target");
        var valueParam = Expression.Parameter(typeof(object), "value");

        // cast target object to concrete declaring type
        var typedTarget = Expression.Convert(targetParam, property.DeclaringType!);

        // cast object value to concrete property type
        var typedValue = Expression.Convert(valueParam, property.PropertyType);

        // generate assignment expression: ((TTarget)target).Property = (TProperty)value
        var propertyAccess = Expression.Property(typedTarget, property);
        var assignExpression = Expression.Assign(propertyAccess, typedValue);

        return Expression.Lambda<Action<object, object?>>(assignExpression, targetParam, valueParam).Compile();
    }
}
