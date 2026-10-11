namespace Telegram.BotKit.Shared.Converters;

/// <summary>
/// Defines a contract for converting string input into specific target types.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Converts input within the given context into the target type.
    /// </summary>
    /// <exception cref="FormatException">Thrown when the input format is invalid.</exception>
    /// <exception cref="NotSupportedException">Thrown when conversion to the target type is not supported.</exception>
    object? Convert(ValueConversionContext context);

    /// <summary>
    /// Attempts to convert input within the given context into the target type without throwing exceptions.
    /// </summary>
    /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
    bool TryConvert(ValueConversionContext context, out object? result);

    /// <summary>
    /// Convenience shortcut for direct value conversion.
    /// </summary>
    object? Convert(string input, Type targetType, object? targetObject = null) =>
        Convert(new ValueConversionContext(input, targetType, targetObject));

    /// <summary>
    /// Convenience shortcut for safe value conversion.
    /// </summary>
    bool TryConvert(string input, Type targetType, out object? result) =>
        TryConvert(new ValueConversionContext(input, targetType), out result);
}