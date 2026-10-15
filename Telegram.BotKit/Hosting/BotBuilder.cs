using System.Security.Cryptography;
using System.Text;
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
using Telegram.BotKit.Hosting.Background;
using Telegram.BotKit.Hosting.Webhook;
using Telegram.BotKit.Shared.Exceptions;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Configures host services, pipelines, and runtime modes for the Telegram bot.
/// </summary>
public sealed class BotBuilder
{
    // host instance for polling mode
    private readonly HostApplicationBuilder? _hostBuilder;

    // host instance for webhook mode
    private readonly WebApplicationBuilder? _webBuilder;

    private readonly BotMode _mode;

    public IServiceCollection Services => _hostBuilder is not null ? _hostBuilder.Services : _webBuilder!.Services;
    public IConfigurationManager Configuration => _hostBuilder is not null ? _hostBuilder.Configuration : _webBuilder!.Configuration;
    public IHostEnvironment Environment => _hostBuilder is not null ? _hostBuilder.Environment : _webBuilder!.Environment;
    public ILoggingBuilder Logging => _hostBuilder is not null ? _hostBuilder.Logging : _webBuilder!.Logging;
    public IMetricsBuilder Metrics => _hostBuilder is not null ? _hostBuilder.Metrics : _webBuilder!.Metrics;
    public HostApplicationBuilder? HostBuilder => _hostBuilder;
    public WebApplicationBuilder? WebApplicationBuilder => _webBuilder;

    internal BotBuilder(string[] args)
    {
        var envName = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Development;

        // preliminary configuration pass strictly for mode resolution
        var preConfig = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var botSettings = preConfig.GetSection(TelegramBotOptions.SectionName).Get<TelegramBotOptions>();
        var rawMode = preConfig.GetSection(TelegramBotOptions.SectionName)["Mode"];

        if (!string.IsNullOrWhiteSpace(rawMode))
        {
            if (!Enum.TryParse<BotMode>(rawMode, ignoreCase: true, out var parsedMode))
            {
                throw new BotStartupException($"Invalid TelegramBot:Mode '{rawMode}'. Expected 'Polling' or 'Webhook'.");
            }

            _mode = parsedMode;
        }
        else
        {
            _mode = BotMode.Polling;
        }

        if (_mode is BotMode.Webhook)
        {
            var webOptions = new WebApplicationOptions
            {
                Args = args,
                EnvironmentName = envName
            };
            _webBuilder = WebApplication.CreateBuilder(webOptions);
        }
        else
        {
            var hostOptions = new HostApplicationBuilderSettings
            {
                Args = args,
                EnvironmentName = envName
            };
            _hostBuilder = Host.CreateApplicationBuilder(hostOptions);
        }

        // options (validated on start) are registered before the hosted services, so invalid
        // configuration fails the host before the bot tries to connect to Telegram
        Services.AddTelegramBotFramework(Configuration);

        if (_mode is BotMode.Webhook)
        {
            Services.AddHostedService<WebhookStartupService>();
        }
        else
        {
            Services.AddHostedService<PollingService>();
        }
    }

    /// <summary>
    /// Builds the configured application instance.
    /// </summary>
    public BotApplication Build()
    {
        if (_mode is not BotMode.Webhook)
        {
            return new BotApplication(_hostBuilder!.Build());
        }

        var app = _webBuilder!.Build();
        var options = app.Services.GetRequiredService<IOptions<TelegramBotOptions>>().Value;

        var routePath = "/api/bot";
        if (Uri.TryCreate(options.WebhookUrl, UriKind.Absolute, out var webhookUri) && !string.IsNullOrWhiteSpace(webhookUri.AbsolutePath))
        {
            routePath = webhookUri.AbsolutePath;
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Telegram.BotKit.Webhook");

        app.MapPost(routePath, (
            HttpContext context,
            IUpdateHandler updateHandler,
            ITelegramBotClient botClient,
            CancellationToken cancellationToken) =>
            WebhookEndpoint.HandleAsync(context, options.SecretToken, updateHandler, botClient, logger, cancellationToken));

        return new BotApplication(app);
    }
}
