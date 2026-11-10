namespace Telegram.BotKit.WidgetQueries.Attributes;

/// <summary>
/// Customizes how a property of a widget query parameter model is mapped to the query data
/// ("key?name=value&amp;name2=value2", the same format as callback data). Every public property of the model that has a
/// setter is a parameter; properties without the attribute are bound by their property name.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class WidgetParamAttribute : Attribute
{
    /// <summary>
    /// Gets the name used in the query data. If null, the property name is used. Names are case-insensitive.
    /// A widget sends 1-64 bytes of data per request, so short names (e.g. "o") help to fit more parameters.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets a value indicating whether the parameter is required.
    /// If true, the binder throws an exception when the value is missing from the query data.
    /// </summary>
    public bool Required { get; init; }
}
