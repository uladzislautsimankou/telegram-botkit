using System.Text;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Callbacks.Building;

/// <summary>
/// Builds callback data strings for inline keyboard buttons, matching what the callback binder reads.
/// </summary>
/// <remarks>
/// Values are written in compact invariant forms to save the 64 bytes: an enum as its number, a Guid without hyphens,
/// a bool as "true" or "false". A DateTime is written as a date when its time is midnight and otherwise to the second
/// (yyyy-MM-ddTHH:mm:ss): its Kind and fractional seconds are not kept, and it is read back with the Kind Unspecified.
/// When the time zone offset matters, use DateTimeOffset or a custom <see cref="Telegram.BotKit.Shared.Converters.IValueConverter"/>
/// (see <see cref="ICallbackDataFactory"/>).
/// </remarks>
public static class CallbackData
{
    /// <summary>
    /// Telegram's limit for callback data, in bytes of its UTF-8 representation.
    /// </summary>
    public const int MaxLengthInBytes = 64;

    /// <summary>
    /// Creates callback data for a handler that takes no parameters.
    /// </summary>
    /// <typeparam name="THandler">The handler the button is routed to.</typeparam>
    /// <returns>The callback data: the route key of the handler.</returns>
    public static string Create<THandler>() where THandler : ICallbackHandler<EmptyCallbackParams> =>
        Create(THandler.Key, new EmptyCallbackParams());

    /// <summary>
    /// Creates callback data for a handler: its route key plus the parameters model.
    /// Values equal to the model defaults are omitted to save space.
    /// </summary>
    /// <typeparam name="THandler">The handler the button is routed to.</typeparam>
    /// <typeparam name="TParams">The parameter model of the handler.</typeparam>
    /// <param name="parameters">The parameter values to encode.</param>
    /// <returns>The callback data: the route key, followed by "?" and the parameters that differ from the defaults.</returns>
    /// <exception cref="ArgumentException">Thrown when a required parameter is null.</exception>
    /// <exception cref="CallbackDataTooLongException">Thrown when the data exceeds <see cref="MaxLengthInBytes"/>.</exception>
    public static string Create<THandler, TParams>(TParams parameters)
        where THandler : ICallbackHandler<TParams>
        where TParams : class, new() =>
        Create(THandler.Key, parameters);

    /// <summary>
    /// Creates callback data from an explicit route key and a parameters model.
    /// </summary>
    /// <typeparam name="TParams">The parameter model.</typeparam>
    /// <param name="key">The route key.</param>
    /// <param name="parameters">The parameter values to encode.</param>
    /// <returns>The callback data: the route key, followed by "?" and the parameters that differ from the defaults.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is empty or contains '?', or a required parameter is null.</exception>
    /// <exception cref="CallbackDataTooLongException">Thrown when the data exceeds <see cref="MaxLengthInBytes"/>.</exception>
    public static string Create<TParams>(string key, TParams parameters) where TParams : class, new() =>
        Create(key, parameters, converters: null);

    /// <summary>
    /// Creates callback data, formatting values with the given converters where they can.
    /// </summary>
    /// <typeparam name="TParams">The parameter model.</typeparam>
    /// <param name="key">The route key.</param>
    /// <param name="parameters">The parameter values to encode.</param>
    /// <param name="converters">The converters asked to format values first, the highest priority first; null for none.</param>
    /// <returns>The callback data: the route key, followed by "?" and the parameters that differ from the defaults.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is empty or contains '?', or a required parameter is null.</exception>
    /// <exception cref="CallbackDataTooLongException">Thrown when the data exceeds <see cref="MaxLengthInBytes"/>.</exception>
    internal static string Create<TParams>(string key, TParams parameters, IReadOnlyList<IValueConverter>? converters)
        where TParams : class, new()
    {
        var data = CallbackDataBuilder.Build(key, parameters, converters);

        var byteCount = Encoding.UTF8.GetByteCount(data);
        if (byteCount > MaxLengthInBytes)
        {
            throw new CallbackDataTooLongException(data, byteCount, MaxLengthInBytes);
        }

        return data;
    }
}
