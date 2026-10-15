namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Specifies the classification of a pipeline execution step.
/// </summary>
public enum PipelineStepType
{
    Routing,
    UserMiddleware,
    Execution
}
