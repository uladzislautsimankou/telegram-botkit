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
    bool TryConvert(string input, Type targetType, out object? result);

    /// <summary>
    /// Attempts to convert a string value using rich context information (e.g., dependency injection services, target object instance).
    /// <para>
    /// The default implementation delegates to <see cref="TryConvert(string, Type, out object?)"/> for backward compatibility.
    /// Override this method if you need access to <see cref="ValueConversionContext.Services"/> (e.g., for database lookups) or <see cref="ValueConversionContext.TargetObject"/> (for cross-property validation).
    /// </para>
    /// </summary>
    /// <param name="context">The context containing the input value, target type, and additional binding metadata.</param>
    /// <param name="result">When this method returns, contains the converted value if the conversion succeeded.</param>
    /// <returns><c>true</c> if the conversion was successful; <c>false</c> if this converter cannot handle the specified type or input.</returns>
    bool TryConvert(ValueConversionContext context, out object? result) => TryConvert(context.Input, context.TargetType, out result);
}
