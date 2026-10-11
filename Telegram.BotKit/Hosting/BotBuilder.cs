using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
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
    public IServiceCollection Services => _hostBuilder is not null ? _hostBuilder.Services : _webBuilder!.Services;

    /// <summary>
    /// Gets the configuration manager for accessing application settings (appsettings.json, environment variables, etc.).
    /// </summary>
    public IConfigurationManager Configuration => _hostBuilder is not null ? _hostBuilder.Configuration : _webBuilder!.Configuration;

    /// <summary>
    /// Gets information about the hosting environment (e.g., Development or Production).
    /// </summary>
    public IHostEnvironment Environment => _hostBuilder is not null ? _hostBuilder.Environment : _webBuilder!.Environment;

    /// <summary>
    /// Gets the logging builder for configuring logging providers and filters.
    /// </summary>
    public ILoggingBuilder Logging => _hostBuilder is not null ? _hostBuilder.Logging : _webBuilder!.Logging;

    /// <summary>
    /// Gets the metrics builder for configuring OpenTelemetry and other metrics.
    /// </summary>
    public IMetricsBuilder Metrics => _hostBuilder is not null ? _hostBuilder.Metrics : _webBuilder!.Metrics;

    /// <summary>
    /// Gets the underlying <see cref="HostApplicationBuilder"/> if running in Polling mode.
    /// Returns null if running in Webhook mode.
    /// </summary>
    public HostApplicationBuilder? HostBuilder => _hostBuilder;

    /// <summary>
    /// Gets the underlying <see cref="WebApplicationBuilder"/> if running in Webhook mode.
    /// Returns null if running in Polling mode.
    /// Use this to configure Kestrel, CORS, or other web-specific settings.
    /// </summary>
    public WebApplicationBuilder? WebApplicationBuilder => _webBuilder;

    internal BotBuilder(string[] args)
    {
        // Determine the environment early to load the correct configuration file.
        var envName =
            System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Development;

        // Build a temporary configuration to read the 'TelegramBot' settings before creating the actual host.
        var preConfig = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
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
            var opts = app.Services.GetRequiredService<IOptions<TelegramBotOptions>>().Value;

            // Dynamically extract the path from the full WebhookUrl configuration
            var routePath = "/api/bot";
            if (Uri.TryCreate(opts.WebhookUrl, UriKind.Absolute, out var webhookUri))
            {
                routePath = webhookUri.AbsolutePath;
            }

            app.MapPost(routePath, async (
                HttpContext context,
                Update update,
                IUpdateHandler updateHandler,
                ITelegramBotClient botClient,
                CancellationToken cancellationToken) =>
            {
                if (!string.IsNullOrEmpty(opts.SecretToken))
                {
                    if (!context.Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var headerToken)
                        || headerToken != opts.SecretToken)
                    {
                        return Results.Unauthorized();
                    }
                }

                await updateHandler.HandleUpdateAsync(botClient, update, cancellationToken);
                return Results.Ok();
            });

            host = app;
        }
        else
        {
            host = _hostBuilder!.Build();
        }

        return new BotApplication(host);
    }
}