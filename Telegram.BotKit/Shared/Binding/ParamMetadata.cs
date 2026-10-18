using System.Reflection;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Shared.Binding;

/// <summary>
/// Metadata representing a bindable property along with its compiled setter delegate.
/// </summary>
/// <param name="PropertyInfo">The property of the parameter model.</param>
/// <param name="Name">The explicit parameter name, or null to use the property name.</param>
/// <param name="Position">The binding order among positional values; negative for a named-only parameter.</param>
/// <param name="Required">True if binding fails when the value is missing.</param>
/// <param name="PropertyType">The type of the property.</param>
/// <param name="Converter">The value converter used to convert the raw input.</param>
/// <param name="Setter">The compiled delegate that assigns the converted value to the property.</param>
/// <param name="TakesRemainder">True for the last positional parameter when it is a string: it receives all remaining positional values joined by a space. Computed once when the binding plan is built.</param>
internal sealed record ParamMetadata(
    PropertyInfo PropertyInfo,
    string? Name,
    int Position,
    bool Required,
    Type PropertyType,
    IValueConverter Converter,
    Action<object, object?> Setter,
    bool TakesRemainder = false)
{
    /// <summary>
    /// Gets the name under which the parameter is supplied as "name=value".
    /// </summary>
    public string Key => Name ?? PropertyInfo.Name;

    /// <summary>
    /// Gets a value indicating whether the parameter can be bound from a positional value.
    /// </summary>
    public bool IsPositional => Position >= 0;
}

