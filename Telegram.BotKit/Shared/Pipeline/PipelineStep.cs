namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Represents a step inside the middleware pipeline.
/// </summary>
internal sealed record PipelineStep(PipelineStepType Type, Type MiddlewareType);
