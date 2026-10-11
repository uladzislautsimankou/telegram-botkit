namespace Telegram.BotKit.Shared.Converters;

/// <summary>
/// Defines an internal strategy for converting string input to a specific target type.
/// </summary>
internal interface IConversionStrategy
{
    bool CanConvert(Type targetType);

    /// <summary>
    /// Converts input to the target type or throws a descriptive FormatException.
    /// </summary>
    object Convert(string input, Type targetType);
}