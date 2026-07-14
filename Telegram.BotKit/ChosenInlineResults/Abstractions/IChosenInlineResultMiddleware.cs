using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared;

namespace Telegram.BotKit.ChosenInlineResults.Abstractions;

/// <summary>
/// Represents a middleware component that processes chosen inline results in the pipeline.
/// </summary>
public interface IChosenInlineResultMiddleware
{
    /// <summary>
    /// Executes the middleware logic.
    /// </summary>
    Task InvokeAsync(ChosenInlineResultContext context, NextDelegate next, CancellationToken cancellationToken = default);
}