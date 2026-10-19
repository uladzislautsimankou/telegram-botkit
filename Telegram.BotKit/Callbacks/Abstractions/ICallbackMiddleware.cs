using Telegram.BotKit.Callbacks.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Callbacks.Abstractions;

/// <summary>
/// Represents a middleware component that processes callback queries in the pipeline.
/// </summary>
public interface ICallbackMiddleware : IPipelineMiddleware<CallbackContext>;
