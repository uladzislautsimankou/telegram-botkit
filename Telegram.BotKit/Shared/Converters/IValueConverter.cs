using System.Diagnostics.CodeAnalysis;

namespace Telegram.BotKit.Shared.Converters;

/// <summary>
/// Defines a contract for converting string input into specific target types.
/// When several registered converters support a type, the one registered last wins,
/// so an application can override the default conversion of any type.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Determines whether the converter supports the specified target type.
    /// </summary>
    /// <param name="targetType">The type to convert to.</param>
    /// <returns>True if the converter supports the type; otherwise, false.</returns>
    bool CanConvert(Type targetType);

    /// <summary>
    /// Converts input within the given context into the target type.
    /// </summary>
    /// <param name="context">The input, the target type and the model being populated.</param>
    /// <returns>The converted value; null for an empty input and a nullable target type.</returns>
    /// <exception cref="FormatException">Thrown when the input format is invalid.</exception>
    /// <exception cref="NotSupportedException">Thrown when conversion to the target type is not supported.</exception>
    object? Convert(ValueConversionContext context);

    /// <summary>
    /// Attempts to convert input within the given context into the target type without throwing exceptions.
    /// </summary>
    /// <param name="context">The input, the target type and the model being populated.</param>
    /// <param name="result">The converted value when the conversion succeeded; otherwise, null.</param>
    /// <returns>True if the input was converted; otherwise, false.</returns>
    bool TryConvert(ValueConversionContext context, out object? result);

    /// <summary>
    /// Formats a value as the text that <see cref="Convert(ValueConversionContext)"/> reads back. It is used to build
    /// callback data with <c>ICallbackDataFactory</c>, so that a type that has its own converter can be put into a
    /// callback button. The default implementation formats nothing, and the built-in formatting applies
    /// (the invariant text of the value, which the default converter reads back). Only values of types the converter
    /// supports (<see cref="CanConvert"/>) are offered. As with conversion, the converter registered last wins.
    /// </summary>
    /// <param name="value">The value to format, never null.</param>
    /// <param name="text">The text form of the value when it was formatted. Any characters are allowed, the structural ones are escaped.</param>
    /// <returns>True if the converter formatted the value; false to leave it to the next converter or the built-in formatting.</returns>
    bool TryFormat(object value, [NotNullWhen(true)] out string? text)
    {
        text = null;
        return false;
    }

    /// <summary>
    /// Convenience shortcut for direct value conversion.
    /// </summary>
    /// <param name="input">The raw text to convert.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="targetObject">The model being populated, or null.</param>
    /// <returns>The converted value; null for an empty input and a nullable target type.</returns>
    /// <exception cref="FormatException">Thrown when the input format is invalid.</exception>
    /// <exception cref="NotSupportedException">Thrown when conversion to the target type is not supported.</exception>
    object? Convert(string input, Type targetType, object? targetObject = null) =>
        Convert(new ValueConversionContext(input, targetType, targetObject));

    /// <summary>
    /// Convenience shortcut for safe value conversion.
    /// </summary>
    /// <param name="input">The raw text to convert.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="result">The converted value when the conversion succeeded; otherwise, null.</param>
    /// <returns>True if the input was converted; otherwise, false.</returns>
    bool TryConvert(string input, Type targetType, out object? result) =>
        TryConvert(new ValueConversionContext(input, targetType), out result);
}
