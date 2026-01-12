using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Example.Middlewares;
using Telegram.BotKit.Example.Services;
using Telegram.BotKit.Extensions;
using Telegram.BotKit.Hosting;

// 1. Create the builder. 
// It automatically loads configuration from appsettings.json and determines the mode (Polling/Webhook).
var builder = BotApplication.CreateBuilder(args);

// 2. Register handlers from the current assembly.
var assembly = typeof(Program).Assembly;
builder.Services.AddCommandHandlers(assembly);
builder.Services.AddCallbackHandlers(assembly);

// 3. Register Custom Middleware (Optional)
// This middleware will run for every command.
builder.Services.AddTransient<ICommandMiddleware, SimpleCommandLogMiddleware>();

// 4. Override Default Error Handler (Optional)
// If you want custom error messages, register your implementation here.
// Note: It must be registered AFTER AddTelegramBotFramework (which happens inside CreateBuilder),
// but since we are using DI, the last registration wins for single services.
builder.Services.AddTransient<ICommandErrorHandlerMiddleware, MyCustomCommandErrorHandler>();

// 5. Override the Main Update Handler (Global logic)
// This enables GlobalUpdateHandler defined above.
builder.Services.ReplaceUpdateHandler<GlobalUpdateHandler>();

// 6. Build and Run
var app = builder.Build();

await app.RunAsync();