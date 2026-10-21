namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Specifies the classification of a pipeline execution step.
/// </summary>
internal enum PipelineStepType
{
    /// <summary>
    /// The routing step that matches the update to a handler.
    /// </summary>
    Routing,

    /// <summary>
    /// A custom middleware registered by the application.
    /// </summary>
    UserMiddleware,

    /// <summary>
    /// The final step that runs the matched handler (or sends the answer for inline queries).
    /// </summary>
    Execution
}
