namespace Telegram.BotKit.Attributes;

/// <summary>
/// Specifies binding options for an inline query parameter property.
/// Allows mapping inline query arguments by position or name.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InlineQyeryParamAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the parameter used for named binding.
    /// If null, the property name is used as the default key.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the zero-based position of the argument in the inline query string for positional binding.
    /// </summary>
    public int Position { get; init; } = -1;

    /// <summary>
    /// Gets a value indicating whether the parameter is required.
    /// If true, the binder will throw an exception if the value is missing.
    /// </summary>
    public bool Required { get; init; } = false;
}
