using System;
using System.Collections.Generic;
using System.Text;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Converters;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Callbacks.Building;

/// <summary>
/// Builds callback data like <see cref="CallbackData"/>, but formats parameter values with the registered
/// <see cref="IValueConverter"/>s too (see <see cref="IValueConverter.TryFormat"/>),
/// so that parameters of types with a custom converter can be put into a button.
/// Inject it where the static <see cref="CallbackData"/> is not enough.
/// </summary>
public interface ICallbackDataFactory
{
    /// <summary>
    /// Creates callback data for a handler that takes no parameters.
    /// </summary>
    /// <typeparam name="THandler">The handler the button is routed to.</typeparam>
    /// <returns>The callback data: the route key of the handler.</returns>
    string Create<THandler>() where THandler : ICallbackHandler<EmptyCallbackParams>;

    /// <summary>
    /// Creates callback data for a handler: its route key plus the parameters model.
    /// Values equal to the model defaults are omitted to save space.
    /// </summary>
    /// <typeparam name="THandler">The handler the button is routed to.</typeparam>
    /// <typeparam name="TParams">The parameter model of the handler.</typeparam>
    /// <param name="parameters">The parameter values to encode.</param>
    /// <returns>The callback data: the route key, followed by "?" and the parameters that differ from the defaults.</returns>
    /// <exception cref="ArgumentException">Thrown when a required parameter is null.</exception>
    /// <exception cref="CallbackDataTooLongException">Thrown when the data exceeds <see cref="CallbackData.MaxLengthInBytes"/>.</exception>
    string Create<THandler, TParams>(TParams parameters)
        where THandler : ICallbackHandler<TParams>
        where TParams : class, new();

    /// <summary>
    /// Creates callback data from an explicit route key and a parameters model.
    /// </summary>
    /// <typeparam name="TParams">The parameter model.</typeparam>
    /// <param name="key">The route key.</param>
    /// <param name="parameters">The parameter values to encode.</param>
    /// <returns>The callback data: the route key, followed by "?" and the parameters that differ from the defaults.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is empty or contains '?', or a required parameter is null.</exception>
    /// <exception cref="CallbackDataTooLongException">Thrown when the data exceeds <see cref="CallbackData.MaxLengthInBytes"/>.</exception>
    string Create<TParams>(string key, TParams parameters) where TParams : class, new();
}

