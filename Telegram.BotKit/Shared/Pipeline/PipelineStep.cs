namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Represents a step inside the middleware pipeline.
/// </summary>
/// <param name="Type">The kind of the step.</param>
/// <param name="MiddlewareType">The middleware type that runs in the step.</param>
internal sealed record PipelineStep(PipelineStepType Type, Type MiddlewareType);
