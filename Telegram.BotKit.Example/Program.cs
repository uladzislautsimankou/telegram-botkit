using Telegram.Bot.Polling;
using Telegram.BotKit.Example.Converters;
using Telegram.BotKit.Example.Middlewares;
using Telegram.BotKit.Example.Services;
using Telegram.BotKit.Hosting;
using Telegram.BotKit.Shared.Converters;


// create the builder (automatically loads configurations and selects the hosting mode)
var builder = BotApplication.CreateBuilder(args);

builder.Services.AddCommands(pipeline =>
{
    pipeline.UseMiddleware<SimpleCommandLogMiddleware>();
    pipeline.UseRouting();
    pipeline.UseMiddleware<MyCustomCommandErrorHandler>();
});

builder.Services.AddCallbacks();

builder.Services.AddInlineQueries(pipeline =>
{
    pipeline.UseMiddleware<MyCustomInlineQueryErrorHandler>();
    pipeline.UseRouting();
    pipeline.UseAnswer<LoggingInlineQueryAnswerMiddleware>();
});

builder.Services.AddMessages(pipeline =>
{
    pipeline.UseMiddleware<UserStateMiddleware>();
    pipeline.UseRouting();
});

// register a custom converter (optional)
builder.Services.AddTransient<IValueConverter, CustomDateConverter>();

// override the main update handler (global logic)
builder.Services.AddSingleton<IUpdateHandler, GlobalUpdateHandler>();

// build and run
var app = builder.Build();

await app.RunAsync();
