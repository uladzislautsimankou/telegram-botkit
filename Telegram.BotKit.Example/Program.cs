using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Example.Converters;
using Telegram.BotKit.Example.Middlewares;
using Telegram.BotKit.Example.Services;
using Telegram.BotKit.Extensions;
using Telegram.BotKit.Hosting;

// create the builder (automatically loads configurations and selects the hosting mode)
var builder = BotApplication.CreateBuilder(args);

// register handlers from the current assembly
var assembly = typeof(Program).Assembly;
builder.Services.AddCommandHandlers(assembly);
builder.Services.AddCallbackHandlers(assembly);
builder.Services.AddMessageHandlers(assembly);
builder.Services.AddInlineQueryHandlers(assembly);

// register custom middleware (optional)
builder.Services.AddTransient<ICommandMiddleware, SimpleCommandLogMiddleware>();

// override default error handler (optional)
builder.Services.AddTransient<ICommandErrorHandlerMiddleware, MyCustomCommandErrorHandler>();
builder.Services.AddTransient<IInlineQueryErrorHandlerMiddleware, MyCustomInlineQueryErrorHandler>();

// register a custom converter (optional)
builder.Services.AddTransient<IValueConverter, CustomDateConverter>();

// override the main update handler (global logic)
builder.Services.ReplaceUpdateHandler<GlobalUpdateHandler>();

// build and run
var app = builder.Build();

await app.RunAsync();