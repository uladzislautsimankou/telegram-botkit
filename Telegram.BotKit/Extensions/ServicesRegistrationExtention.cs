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
        // bind and configure telegram bot options
        services.Configure<TelegramBotOptions>(configuration.GetSection("TelegramBot"));

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

        // register the main update handler receiving and distributing updates
        services.TryAddSingleton<IUpdateHandler, DefaultUpdateHandler>();

        // register default error handlers for various update types
        services.TryAddTransient<ICommandErrorHandlerMiddleware, DefaultCommandErrorHandler>();
        services.TryAddTransient<ICallbackErrorHandlerMiddleware, DefaultCallbackErrorHandler>();
        services.TryAddTransient<IInlineQueryErrorHandlerMiddleware, DefaultInlineQueryErrorHandler>();
        services.TryAddTransient<IMessageErrorHandlerMiddleware, DefaultMessageErrorHandler>();

        // register default state middleware
        services.TryAddTransient<IMessageStateMiddleware, DefaultUserStateMiddleware>();

        // register dispatchers to orchestrate the pipeline for each update type
        services.AddTransient<ICommandDispatcher, CommandDispatcher>();
        services.AddTransient<ICallbackDispatcher, CallbackDispatcher>();
        services.AddTransient<IInlineQueryDispatcher, InlineQueryDispatcher>();
        services.AddTransient<IMessageDispatcher, MessageDispatcher>();

        // register command pipeline middlewares
        services.AddTransient<CommandRoutingMiddleware>();
        services.AddTransient<CommandExecutionMiddleware>();

        // register callback and inline query pipeline middlewares
        services.AddTransient<CallbackRoutingMiddleware>();
        services.AddTransient<InlineQueryRoutingMiddleware>();

        // register message pipeline middlewares
        services.AddTransient<MessageRoutingMiddleware>();
        services.AddTransient<MessageExecutionMiddleware>();

        // register parameter binders and converters for model binding from incoming updates
        services.TryAddEnumerable(ServiceDescriptor.Transient<IValueConverter, DefaultValueConverter>());

        services.AddTransient<ICallbackParameterBinder, CallbackParameterBinder>();
        services.AddTransient<ICommandParameterBinder, CommandParameterBinder>();
        services.AddTransient<IInlineQueryParameterBinder, InlineQueryParameterBinder>();

        // register bot metadata and identification services
        services.AddSingleton<BotInfo>();
        services.AddSingleton<IBotInfo>(sp => sp.GetRequiredService<BotInfo>());

        return services;
    }
}
