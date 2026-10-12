namespace Telegram.BotKit.Shared.Pipeline;

/// <summary>
/// Represents the compiled entry point of an update processing pipeline.
/// </summary>
/// <typeparam name="TContext">The update context type flowing through the pipeline.</typeparam>
/// <param name="context">The contextual data for the incoming update.</param>
/// <param name="serviceProvider">The scoped service provider used for resolving dependencies.</param>
/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
/// <returns>A task that represents the completion of the entire pipeline branch.</returns>
internal delegate ValueTask PipelineDelegate<in TContext>(
    TContext context,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken);