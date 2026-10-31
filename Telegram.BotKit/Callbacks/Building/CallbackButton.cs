using Telegram.Bot.Types.ReplyMarkups;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Callbacks.Building;

/// <summary>
/// Creates inline keyboard buttons whose callback data is routed to a specific handler.
/// </summary>
public static class CallbackButton
{
    /// <summary>
    /// Creates a button for a handler that takes no parameters.
    /// </summary>
    /// <typeparam name="THandler">The handler the button is routed to.</typeparam>
    /// <param name="text">The text of the button.</param>
    /// <returns>An inline keyboard button whose callback data is routed to <typeparamref name="THandler"/>.</returns>
    public static InlineKeyboardButton Create<THandler>(string text)
        where THandler : ICallbackHandler<EmptyCallbackParams> =>
        InlineKeyboardButton.WithCallbackData(text, CallbackData.Create<THandler>());

    /// <summary>
    /// Creates a button routed to <typeparamref name="THandler"/> with the given parameters.
    /// </summary>
    /// <typeparam name="THandler">The handler the button is routed to.</typeparam>
    /// <typeparam name="TParams">The parameter model of the handler.</typeparam>
    /// <param name="text">The text of the button.</param>
    /// <param name="parameters">The parameter values to encode in the callback data.</param>
    /// <returns>An inline keyboard button whose callback data is routed to <typeparamref name="THandler"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when a required parameter is null.</exception>
    /// <exception cref="CallbackDataTooLongException">Thrown when the callback data exceeds <see cref="CallbackData.MaxLengthInBytes"/>.</exception>
    public static InlineKeyboardButton Create<THandler, TParams>(string text, TParams parameters)
        where THandler : ICallbackHandler<TParams>
        where TParams : class, new() =>
        InlineKeyboardButton.WithCallbackData(text, CallbackData.Create<THandler, TParams>(parameters));
}
