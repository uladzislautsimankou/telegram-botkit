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
using Telegram.Bot.Types;
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
            _webBuilder.Services.AddHostedService<WebhookStartupService>();
        }
        else
        {
            var hostOptions = new HostApplicationBuilderSettings
            {
                Args = args,
                EnvironmentName = envName
            };
            _hostBuilder = Host.CreateApplicationBuilder(hostOptions);
            _hostBuilder.Services.AddHostedService<PollingService>();
        }

        Services.AddTelegramBotFramework(Configuration);
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

        app.MapPost(routePath, async (
            HttpContext context,
            Update update,
            IUpdateHandler updateHandler,
            ITelegramBotClient botClient,
            CancellationToken cancellationToken) =>
        {
            if (!string.IsNullOrEmpty(options.SecretToken))
            {
                if (!context.Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var tokenHeader)
                    || !IsSecretTokenValid(tokenHeader.ToString(), options.SecretToken))
                {
                    return Results.Unauthorized();
                }
            }

            await updateHandler.HandleUpdateAsync(botClient, update, cancellationToken);
            return Results.Ok();
        });

        return new BotApplication(app);
    }

    private static bool IsSecretTokenValid(string providedToken, string expectedToken)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
