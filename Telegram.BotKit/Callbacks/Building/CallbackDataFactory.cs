using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Converters;

namespace Telegram.BotKit.Callbacks.Building;

/// <summary>
/// Builds callback data using the registered value converters to format parameter values.
/// </summary>
/// <param name="converters">The registered value converters; the one registered last has the highest priority.</param>
internal sealed class CallbackDataFactory(IEnumerable<IValueConverter> converters) : ICallbackDataFactory
{
    // the converter registered last wins, as it does when the data is read back
    private readonly IValueConverter[] _converters = converters.Reverse().ToArray();

    /// <inheritdoc />
    public string Create<THandler>() where THandler : ICallbackHandler<EmptyCallbackParams> =>
        Create(THandler.Key, new EmptyCallbackParams());

    /// <inheritdoc />
    public string Create<THandler, TParams>(TParams parameters)
        where THandler : ICallbackHandler<TParams>
        where TParams : class, new() =>
        Create(THandler.Key, parameters);

    /// <inheritdoc />
    public string Create<TParams>(string key, TParams parameters) where TParams : class, new() =>
        CallbackData.Create(key, parameters, _converters);
}
