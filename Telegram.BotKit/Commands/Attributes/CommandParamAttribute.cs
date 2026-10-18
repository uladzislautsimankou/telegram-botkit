namespace Telegram.BotKit.Commands.Attributes;

/// <summary>
/// Specifies binding options for a command parameter property.
/// Allows mapping command arguments by position or name.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CommandParamAttribute : Attribute
{
    /// <summary>
    /// Gets the name under which the parameter can be supplied as "name=value" (e.g. "age" in "/cmd age=25").
    /// If null, the property name is used. Names are case-insensitive.
    /// A token "x=y" is a named argument only when "x" is a known parameter name of the model; any other token
    /// stays a positional value, so "/calc 2+2=4" passes "2+2=4" as is.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the position of the parameter among the positional values, i.e. the arguments given without "name=":
    /// in "/remind buy milk when=18:00" "buy" and "milk" are positional, "when" is named.
    /// The value defines the binding order only and is not an index: positions must be unique but may have gaps
    /// (0, 5, 10 binds exactly like 0, 1, 2), and a parameter supplied by name does not consume a positional value.
    /// If the last positional parameter is a string it receives all remaining positional values joined by a space
    /// (here "buy milk"). If it is of any other type, positional values beyond the last positional parameter
    /// are ignored without an error: with two positional <c>int</c> parameters "/sum 1 2 3" binds 1 and 2 and drops 3.
    /// A negative value (the default) makes the parameter named-only.
    /// </summary>
    public int Position { get; init; } = -1;

    /// <summary>
    /// Gets a value indicating whether the parameter is required.
    /// If true, the binder throws an exception when the value is missing.
    /// </summary>
    public bool Required { get; init; }
}

