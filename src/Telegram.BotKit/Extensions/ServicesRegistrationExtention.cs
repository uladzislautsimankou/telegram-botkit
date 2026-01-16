using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;
using Telegram.BotKit.Binding.Converters;
using Telegram.BotKit.Configuration;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Pipeline.Dispatchers;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Extensions;

internal static class ServicesRegistrationExtention
{
    public static IServiceCollection AddTelegramBotFramework(this IServiceCollection services, IConfiguration configuration)
    {
        // конфигурации
        services.Configure<TelegramBotOptions>(configuration.GetSection("TelegramBot"));

        // клиент бота
        services.AddHttpClient("tg_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                var opts = sp.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
                return new TelegramBotClient(opts.Token, httpClient);
            });

        // хендлеры, роутеры, мидлвары
        services.TryAddSingleton<IUpdateHandler, DefaultUpdateHandler>();

        services.TryAddTransient<ICommandErrorHandlerMiddleware, DefaultCommandErrorHandler>();
        services.TryAddTransient<ICallbackErrorHandlerMiddleware, DefaultCallbackErrorHandler>();

        services.AddTransient<ICommandDispatcher, CommandDispatcher>();
        services.AddTransient<ICallbackDispatcher, CallbackDispatcher>();

        services.AddTransient<CommandRoutingMiddleware>();
        services.AddTransient<CallbackRoutingMiddleware>();

        services.TryAddEnumerable(ServiceDescriptor.Transient<IValueConverter, DefaultValueConverter>());

        services.AddTransient<ICallbackParameterBinder, CallbackParameterBinder>();
        services.AddTransient<ICommandParameterBinder, CommandParameterBinder>();

        services.AddSingleton<BotInfo>();
        services.AddSingleton<IBotInfo>(sp => sp.GetRequiredService<BotInfo>());

        return services;
    }
}
