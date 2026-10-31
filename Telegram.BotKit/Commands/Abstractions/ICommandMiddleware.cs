using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Pipeline;

namespace Telegram.BotKit.Commands.Abstractions;

/// <summary>
/// Represents a middleware component that processes bot commands in the pipeline.
/// </summary>
public interface ICommandMiddleware : IPipelineMiddleware<CommandContext>;
