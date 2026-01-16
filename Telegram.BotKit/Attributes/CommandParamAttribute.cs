namespace Telegram.BotKit.Attributes;

/// <summary>
/// Specifies binding options for a command parameter property.
/// Allows mapping command arguments by position or name.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CommandParamAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the parameter used for named binding (e.g., "age" in "/cmd age=25").
    /// If null, the property name is used as the default key.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the zero-based position of the argument in the command string for positional binding.
    /// </summary>
    public int Position { get; init; } = -1;

    /// <summary>
    /// Gets a value indicating whether the parameter is required.
    /// If true, the binder will throw an exception if the value is missing in the command arguments.
    /// </summary>
    public bool Required { get; init; } = false;
}