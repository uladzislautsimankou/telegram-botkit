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
    // before routing: catches the errors of everything below
    pipeline.UseMiddleware<MyCustomCommandErrorHandler>();
    pipeline.UseRouting();
    // after routing: runs for unmatched commands too
    pipeline.UseMiddleware<CommandRoutingResultMiddleware>();
});

builder.Services.AddCallbacks(pipeline =>
{
    pipeline.UseRouting();
    pipeline.UseMiddleware<CallbackRoutingResultMiddleware>();
});

builder.Services.AddInlineQueries(pipeline =>
{
    pipeline.UseMiddleware<MyCustomInlineQueryErrorHandler>();
    pipeline.UseRouting();
    pipeline.UseAnswer<LoggingInlineQueryAnswerMiddleware>();
});

builder.Services.AddChosenInlineResults();

builder.Services.AddMessages(pipeline =>
{
    pipeline.UseMiddleware<UserStateMiddleware>();
    pipeline.UseRouting();
});

// register a custom converter (optional)
builder.Services.AddTransient<IValueConverter, CustomDateConverter>();

// register the update handler that passes updates to the dispatchers (required)
builder.Services.AddSingleton<IUpdateHandler, GlobalUpdateHandler>();

// build and run
var app = builder.Build();

await app.RunAsync();
