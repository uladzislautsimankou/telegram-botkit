namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Defines a contract for converting string input into specific target types.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Attempts to convert a string value to the specified target type.
    /// </summary>
    /// <param name="input">The input string value to convert.</param>
    /// <param name="targetType">The target type to convert the value to.</param>
    /// <param name="result">When this method returns, contains the converted value if the conversion succeeded.</param>
    /// <returns><c>true</c> if the conversion was successful; <c>false</c> if this converter cannot handle the specified type or input.</returns>
    public bool TryConvert(string input, Type targetType, out object? result);
}
