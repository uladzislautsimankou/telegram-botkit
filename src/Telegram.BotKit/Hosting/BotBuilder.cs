using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.BotKit.Configuration;
using Telegram.BotKit.Extensions;
using Telegram.BotKit.Hosting.Background;
using Telegram.BotKit.Hosting.Webhook;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// A builder for configuring the Telegram bot application, services, and middleware.
/// Automatically handles the choice between Polling and Webhook modes based on configuration.
/// </summary>
public sealed class BotBuilder
{
    private readonly HostApplicationBuilder? _hostBuilder; // Used for Polling mode
    private readonly WebApplicationBuilder? _webBuilder;   // Used for Webhook mode

    private readonly BotMode _mode;

    /// <summary>
    /// Gets the service collection to register application services.
    /// </summary>
    public IServiceCollection Services => _hostBuilder?.Services ?? _webBuilder!.Services;

    /// <summary>
    /// Gets the configuration manager for accessing application settings (appsettings.json, environment variables, etc.).
    /// </summary>
    public IConfigurationManager Configuration => _hostBuilder?.Configuration ?? _webBuilder!.Configuration;

    /// <summary>
    /// Gets information about the hosting environment (e.g., Development or Production).
    /// </summary>
    public IHostEnvironment Environment => _hostBuilder?.Environment ?? _webBuilder!.Environment;

    internal BotBuilder(string[] args)
    {
        // Determine the environment early to load the correct configuration file.
        var envName =
            System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Development;

        // Build a temporary configuration to read the 'TelegramBot' settings before creating the actual host.
        var preConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var botSettings = preConfig.GetSection("TelegramBot").Get<TelegramBotOptions>();

        _mode = botSettings?.Mode ?? BotMode.Polling;

        if (_mode == BotMode.Webhook)
        {
            // Initialize WebApplicationBuilder for Webhook mode (requires Kestrel and Controllers).
            var webOptions = new WebApplicationOptions
            {
                Args = args,
                EnvironmentName = envName
            };

            _webBuilder = WebApplication.CreateBuilder(webOptions);

            _webBuilder.Services.AddControllers();
        }
        else
        {
            // Initialize HostApplicationBuilder for Polling mode (lightweight, no web server).
            var hostOptions = new HostApplicationBuilderSettings
            {
                Args = args,
                EnvironmentName = envName
            };

            _hostBuilder = Host.CreateApplicationBuilder(hostOptions);
        }

        // Register the core framework services.
        Services.AddTelegramBotFramework(Configuration);

        // Register the appropriate startup service based on the selected mode.
        if (_mode == BotMode.Webhook)
        {
            Services.AddHostedService<WebhookStartupService>();
        }
        else
        {
            Services.AddHostedService<PollingService>();
        }
    }

    /// <summary>
    /// Builds the <see cref="BotApplication"/>.
    /// </summary>
    /// <returns>A configured instance of <see cref="BotApplication"/>.</returns>
    public BotApplication Build()
    {
        IHost host;

        if (_mode == BotMode.Webhook)
        {
            var app = _webBuilder!.Build();

            // Map controllers to handle incoming Telegram updates via HTTP POST.
            app.MapControllers();

            host = app;
        }
        else
        {
            host = _hostBuilder!.Build();
        }

        return new BotApplication(host);
    }
}