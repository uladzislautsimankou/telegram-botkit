using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Messages.Abstractions;

/// <summary>
/// Represents a middleware component that processes incoming messages in the pipeline.
/// </summary>
public interface IMessageMiddleware : IPipelineMiddleware<MessageContext>;
