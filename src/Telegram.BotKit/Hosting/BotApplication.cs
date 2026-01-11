using Microsoft.Extensions.Hosting;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Represents the configured Telegram bot application.
/// This class acts as a wrapper around the underlying .NET Host, managing the application's lifetime and execution.
/// </summary>
public sealed class BotApplication
{
    private readonly IHost _host;

    internal BotApplication(IHost host) => _host = host;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotBuilder"/> class with pre-configured defaults.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A new <see cref="BotBuilder"/> instance.</returns>
    public static BotBuilder CreateBuilder(string[] args) => new BotBuilder(args);

    /// <summary>
    /// Runs the application and blocks the calling thread until the host is stopped.
    /// </summary>
    /// <returns>A task that represents the application's lifetime.</returns>
    public async Task RunAsync() => await _host.RunAsync();

    /// <summary>
    /// Gets the application's service provider, used to resolve dependencies.
    /// </summary>
    public IServiceProvider Services => _host.Services;
}
