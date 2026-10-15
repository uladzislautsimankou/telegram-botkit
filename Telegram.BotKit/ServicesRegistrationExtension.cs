using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared.Converters;

namespace Microsoft.Extensions.DependencyInjection;

internal static class ServicesRegistrationExtension
{
    /// <summary>
    /// Registers core Telegram.BotKit framework services, configurations, pipelines, and the <see cref="ITelegramBotClient"/> in the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration instance containing "TelegramBot" options.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance for method chaining.</returns>
    public static IServiceCollection AddTelegramBotFramework(this IServiceCollection services, IConfiguration configuration)
    {
        // bind and configure telegram bot options
        services.Configure<TelegramBotOptions>(configuration.GetSection(TelegramBotOptions.SectionName));

        // register the typed telegram bot client with a dedicated HTTP client
        services.AddHttpClient("tg_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                var opts = sp.GetRequiredService<IOptions<TelegramBotOptions>>().Value;

                var botOptions = !string.IsNullOrWhiteSpace(opts.BaseUrl)
                    ? new TelegramBotClientOptions(opts.Token, opts.BaseUrl)
                    : new TelegramBotClientOptions(opts.Token);

                return new TelegramBotClient(botOptions, httpClient);
            });

        // register parameter binders and converters for model binding from incoming updates
        services.TryAddEnumerable(ServiceDescriptor.Transient<IValueConverter, DefaultValueConverter>());

        // register bot metadata and identification services
        services.AddSingleton<BotInfo>();
        services.AddSingleton<IBotInfo>(sp => sp.GetRequiredService<BotInfo>());

        return services;
    }
}
