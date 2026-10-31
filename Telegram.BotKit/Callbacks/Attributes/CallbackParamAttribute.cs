namespace Telegram.BotKit.Callbacks.Attributes;

/// <summary>
/// Customizes how a property of a callback parameter model is mapped to the callback data
/// ("key?name=value&amp;name2=value2"). Every public property of the model that has both a getter and a setter is a
/// callback parameter; properties without the attribute are bound by their property name.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CallbackParamAttribute : Attribute
{
    /// <summary>
    /// Gets the name used in the callback data. If null, the property name is used. Names are case-insensitive.
    /// Telegram limits callback data to 64 bytes, so short names (e.g. "p") help to fit more parameters.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets a value indicating whether the parameter is required.
    /// If true, the binder throws an exception when the value is missing from the callback data,
    /// and building callback data throws when the value is null.
    /// </summary>
    public bool Required { get; init; }
}


