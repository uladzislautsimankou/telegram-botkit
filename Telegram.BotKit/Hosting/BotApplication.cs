using Microsoft.Extensions.Hosting;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Configured Telegram bot application wrapper around the underlying .NET host.
/// </summary>
public sealed class BotApplication(IHost host)
{
    /// <summary>
    /// Initializes a builder instance with default configurations.
    /// </summary>
    public static BotBuilder CreateBuilder(string[] args) => new(args);

    /// <summary>
    /// Runs the application and blocks the calling thread until the host stops.
    /// </summary>
    public Task RunAsync(CancellationToken cancellationToken = default) => host.RunAsync(cancellationToken);

    /// <summary>
    /// Gets the application dependency injection container.
    /// </summary>
    public IServiceProvider Services => host.Services;
}