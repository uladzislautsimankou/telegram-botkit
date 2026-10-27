using System.Reflection;
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
using Telegram.BotKit.Shared.Startup;

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

    /// <summary>
    /// Gets the service collection of the underlying host builder.
    /// </summary>
    public IServiceCollection Services => _hostBuilder is not null ? _hostBuilder.Services : _webBuilder!.Services;
    /// <summary>
    /// Gets the configuration manager of the underlying host builder.
    /// </summary>
    public IConfigurationManager Configuration => _hostBuilder is not null ? _hostBuilder.Configuration : _webBuilder!.Configuration;
    /// <summary>
    /// Gets the hosting environment of the application.
    /// </summary>
    public IHostEnvironment Environment => _hostBuilder is not null ? _hostBuilder.Environment : _webBuilder!.Environment;
    /// <summary>
    /// Gets the logging builder used to configure the logging of the application.
    /// </summary>
    public ILoggingBuilder Logging => _hostBuilder is not null ? _hostBuilder.Logging : _webBuilder!.Logging;
    /// <summary>
    /// Gets the metrics builder used to configure the metrics of the application.
    /// </summary>
    public IMetricsBuilder Metrics => _hostBuilder is not null ? _hostBuilder.Metrics : _webBuilder!.Metrics;
    /// <summary>
    /// Gets the generic host builder when the bot runs in polling mode; otherwise, null.
    /// </summary>
    public HostApplicationBuilder? HostBuilder => _hostBuilder;
    /// <summary>
    /// Gets the web application builder when the bot runs in webhook mode; otherwise, null.
    /// </summary>
    public WebApplicationBuilder? WebApplicationBuilder => _webBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotBuilder"/> class: reads the bot mode from the configuration,
    /// creates the matching host builder and registers the framework services.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <exception cref="BotStartupException">Thrown when <c>TelegramBot:Mode</c> is neither "Polling" nor "Webhook".</exception>
    internal BotBuilder(string[] args)
    {
        var envName = ResolveEnvironmentName(args);
        var preConfig = BuildPreConfiguration(args, envName);

        var rawMode = preConfig.GetSection(TelegramBotOptions.SectionName)["Mode"];

        if (!string.IsNullOrWhiteSpace(rawMode))
        {
            if (!Enum.TryParse<BotMode>(rawMode, ignoreCase: true, out var parsedMode) || !Enum.IsDefined(parsedMode))
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
            _webBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                EnvironmentName = envName
            });
        }
        else
        {
            _hostBuilder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                EnvironmentName = envName
            });
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

    private static string ResolveEnvironmentName(string[] args)
    {
        // same sources as the default hosts: DOTNET_/ASPNETCORE_ variables and --environment
        var hostConfig = new ConfigurationBuilder()
            .AddEnvironmentVariables("DOTNET_")
            .AddEnvironmentVariables("ASPNETCORE_")
            .AddCommandLine(args)
            .Build();

        var name = hostConfig[HostDefaults.EnvironmentKey];

        // absence of an explicit environment means Production, as in the generic host
        return string.IsNullOrWhiteSpace(name) ? Environments.Production : name;
    }

    private static IConfigurationRoot BuildPreConfiguration(string[] args, string envName)
    {
        // mirrors the default host configuration sources: same order, same content root
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false);

        if (string.Equals(envName, Environments.Development, StringComparison.OrdinalIgnoreCase)
            && Assembly.GetEntryAssembly() is { } entryAssembly)
        {
            builder.AddUserSecrets(entryAssembly, optional: true);
        }

        return builder
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
    }

    /// <summary>
    /// Builds the configured application instance and checks the setup of the bot: an <see cref="IUpdateHandler"/> can be
    /// created and the parameter models of command and callback handlers are valid. All problems are logged and reported
    /// together. The checks only use the built service provider, so they work with any container and in both bot modes.
    /// </summary>
    /// <returns>The configured bot application.</returns>
    /// <exception cref="BotStartupException">Thrown when the setup is invalid; the message lists every problem.</exception>
    public BotApplication Build()
    {
        if (_mode is not BotMode.Webhook)
        {
            var host = _hostBuilder!.Build();
            RunStartupChecks(host.Services);
            return new BotApplication(host);
        }

        var app = _webBuilder!.Build();
        var options = RunStartupChecks(app.Services);

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

    // webhook mode needs the options to map its endpoint, so both modes read them here: invalid configuration
    // fails Build() in either mode, not at host start in one of them
    private static TelegramBotOptions RunStartupChecks(IServiceProvider services)
    {
        StartupValidator.Run(services);
        return services.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
    }
}
