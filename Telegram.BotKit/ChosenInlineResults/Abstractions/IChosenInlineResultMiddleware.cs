using Telegram.BotKit.ChosenInlineResults.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.ChosenInlineResults.Abstractions;

/// <summary>
/// Represents a middleware component that processes chosen inline results in the pipeline.
/// </summary>
public interface IChosenInlineResultMiddleware : IPipelineMiddleware<ChosenInlineResultContext>;
