using Microsoft.Extensions.Hosting;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Configured Telegram bot application wrapper around the underlying .NET host.
/// </summary>
public sealed class BotApplication
{
    private readonly IHost _host;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotApplication"/> class.
    /// </summary>
    /// <param name="host">The built host that runs the bot.</param>
    internal BotApplication(IHost host) => _host = host;

    /// <summary>
    /// Initializes a builder instance with default configurations.
    /// </summary>
    /// <param name="args">The command-line arguments (e.g. <c>--environment=Staging</c>, <c>--TelegramBot:Mode=Webhook</c>).</param>
    /// <returns>A builder that configures the bot application.</returns>
    public static BotBuilder CreateBuilder(string[] args) => new(args);

    /// <summary>
    /// Runs the application. The returned task completes when the host shuts down.
    /// </summary>
    /// <param name="cancellationToken">A token that triggers the shutdown of the host.</param>
    public Task RunAsync(CancellationToken cancellationToken = default) => _host.RunAsync(cancellationToken);

    /// <summary>
    /// Gets the application dependency injection container.
    /// </summary>
    public IServiceProvider Services => _host.Services;
}
