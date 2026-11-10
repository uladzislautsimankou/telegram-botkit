# Telegram.BotKit 🤖
<p align="left">
  <a href="https://www.nuget.org/packages/Telegram.BotKit"><img src="https://img.shields.io/nuget/v/Telegram.BotKit.svg" alt="NuGet Version" /></a>
</p>

A structured .NET 10 framework for building Telegram bots on top of [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot), using dependency injection, strong typing, and middleware pipelines.

---

## 🏗 Architecture

Every update first reaches **your** `IUpdateHandler`, which hands it to the dispatcher of the matching kind. Each dispatcher runs its own isolated pipeline in a dedicated DI scope:

```mermaid
graph TD
    Host[Polling or Webhook host] --> Handler{"Your IUpdateHandler"}

%% Command Flow
    Handler -- "Message starts with /" --> CmdDisp[ICommandDispatcher]
    CmdDisp --> CmdPipe[Pipeline: Middlewares -> Routing -> Middlewares -> Execution]
    CmdPipe --> CmdHandler[ICommandHandler]

%% Callback Flow
    Handler -- Callback Query --> CbkDisp[ICallbackDispatcher]
    CbkDisp --> CbkPipe[Pipeline: Middlewares -> Routing -> Middlewares -> Execution]
    CbkPipe --> CbkHandler[ICallbackHandler]

%% Message Flow
    Handler -- Regular Message --> MsgDisp[IMessageDispatcher]
    MsgDisp --> MsgPipe[Pipeline: Middlewares -> Routing -> Middlewares -> Execution]
    MsgPipe --> MsgHandler[IMessageHandler]

%% Inline Query Flow
    Handler -- Inline Query --> InlDisp[IInlineQueryDispatcher]
    InlDisp --> InlPipe[Pipeline: Middlewares -> Routing -> Middlewares -> Answer]
    InlPipe --> InlHandler["IInlineQueryHandler (all matching handlers)"]

%% Chosen Inline Result Flow
    Handler -- Chosen Inline Result --> ChoDisp[IChosenInlineResultDispatcher]
    ChoDisp --> ChoPipe[Pipeline: Middlewares -> Routing -> Middlewares -> Execution]
    ChoPipe --> ChoHandler[IChosenInlineResultHandler]

%% Widget Query Flow
    Handler -- "Widget Query / Shared Widget Query" --> WidDisp[IWidgetQueryDispatcher]
    WidDisp --> WidPipe[Pipeline: Middlewares -> Routing -> Middlewares -> Execution]
    WidPipe --> WidHandler[IWidgetQueryHandler]
```

---

## ✨ Features

* **Parameter Binding:** Maps command arguments (`/remind buy milk when=18:00`), callback data (`action?id=12`) and widget query data to typed models.
* **Callback Data Builder:** `CallbackButton` builds the callback data of a handler from its typed model and checks Telegram's 64-byte limit.
* **Typo Suggestions:** An unknown command is routed with similar registered commands in `CommandContext.RouteResult`, so a middleware can answer "Did you mean ...?".
* **Modular Pipelines:** Middleware pipelines for Commands, Callbacks, Messages, Inline Queries, Chosen Inline Results, and Widget Queries with an explicit routing position (`UseRouting()`).
* **State Routing:** Message routing by dialog state (`RequiredState`) with fallback to global handlers.
* **Inline Queries:** All matching handlers run (in parallel by default) and their results are merged into one answer.
* **Widget Queries:** Handlers for taps in HTML widgets (Bot API 10.4), routed by the key of the query data. Queries from forwarded copies of shared widgets go through the same pipeline.
* **Startup Validation:** `Build()` fails fast on a missing `IUpdateHandler`, invalid parameter models, or invalid options.
* **Hosting Modes:** Polling and Webhook, selected with a single setting.

---

## 🚀 Quick Start

### 1. Install Package

```bash
dotnet add package Telegram.BotKit
```

### 2. Configure Settings (`appsettings.json`)

```json
{
  "TelegramBot": {
    "Token": "YOUR_BOT_TOKEN_HERE",
    "AllowedUpdates": [ "Message", "CallbackQuery", "InlineQuery", "ChosenInlineResult", "WidgetQuery" ],
    "Mode": "Polling"
  }
}
```

Webhook mode also needs `WebhookUrl` (an HTTPS URL whose path is the route the bot listens on) and, strongly recommended, `SecretToken`. All options are described in the [Example documentation](Telegram.BotKit.Example/README.md). Keep the token out of the repository (user secrets or the `TelegramBot__Token` environment variable).

### 3. Write the Update Handler

BotKit routes updates to handlers, but **you** decide which dispatcher gets which update:

```csharp
public sealed class BotUpdateHandler(
    ICommandDispatcher commands,
    ICallbackDispatcher callbacks,
    IMessageDispatcher messages,
    IInlineQueryDispatcher inlineQueries,
    IChosenInlineResultDispatcher chosenResults,
    IWidgetQueryDispatcher widgetQueries,
    ILogger<BotUpdateHandler> logger) : IUpdateHandler
{
    public Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct) => update switch
    {
        { CallbackQuery: { } query } => callbacks.DispatchAsync(query, ct),
        { Message: { Text: { } text } message } when text.StartsWith('/') => commands.DispatchAsync(message, ct),
        { Message: { } message } => messages.DispatchAsync(message, ct),
        { InlineQuery: { } query } => inlineQueries.DispatchAsync(query, ct),
        { ChosenInlineResult: { } result } => chosenResults.DispatchAsync(result, ct),
        { WidgetQuery: { } query } => widgetQueries.DispatchAsync(query, ct),
        { SharedWidgetQuery: { } query } => widgetQueries.DispatchAsync(query, ct),
        _ => Task.CompletedTask
    };

    public Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, HandleErrorSource source, CancellationToken ct)
    {
        logger.LogError(exception, "Update pipeline failed. Source: {Source}", source);
        return Task.CompletedTask;
    }
}
```

### 4. Application Setup (`Program.cs`)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Polling;
using Telegram.BotKit.Hosting;

var builder = BotApplication.CreateBuilder(args);

builder.Services
    .AddCommands()
    .AddCallbacks()
    .AddMessages()
    .AddInlineQueries()
    .AddChosenInlineResults()
    .AddWidgetQueries();

builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();

var app = builder.Build();
await app.RunAsync();
```

Each `AddXxx()` scans the calling assembly for handlers (pass other assemblies explicitly). Register only the dispatchers your update handler uses.

---

## 🚇 Pipeline Configuration

The pipeline is: middleware before `UseRouting()` → routing → middleware after `UseRouting()` → handler. If `UseRouting()` is not called, routing runs first.

* Middleware before `UseRouting()` runs for every update and wraps everything after it (error handling, loading `MessageContext.UserState`).
* Middleware after `UseRouting()` runs **even when no handler matched**: check the routing result (`CommandContext.RouteResult`, `CallbackContext.MatchedCallbackMetadata`, `WidgetQueryContext.MatchedWidgetQueryMetadata`, ...).
* BotKit never replies on its own: unknown commands, binding errors, and handler exceptions are answered by your middleware.

```csharp
builder.Services.AddCommands(pipeline =>
{
    pipeline.UseMiddleware<CommandErrorMiddleware>();          // runs for all command attempts
    pipeline.UseRouting();                                     // matches the command against registered handlers
    pipeline.UseMiddleware<CommandRoutingResultMiddleware>();  // reacts to matched / not matched commands
});

builder.Services.AddInlineQueries(pipeline =>
{
    pipeline.UseRouting();
    pipeline.UseAnswer<LoggingInlineQueryAnswerMiddleware>();  // replaces the default answer step
});
```

---

## 🧩 Widget Queries

An HTML widget (`sendWidget`, Bot API 10.4) asks the bot with `TelegramWidget.request(data)`: a button inside the widget that works like a callback button. The query is routed by the key of its `data`, which has the format of callback data (`counter:add?by=5`, 1-64 bytes):

```csharp
public sealed record CounterParams
{
    [WidgetParam(Name = "by")]
    public int By { get; init; } = 1;
}

public sealed class CounterWidgetQueryHandler(ITelegramBotClient bot) : IWidgetQueryHandler<CounterParams>
{
    public static string Key => "counter:add";

    public async Task HandleAsync(CounterParams parameters, WidgetQueryContext context, CancellationToken ct)
    {
        var total = Add(parameters.By);

        // text for the widget that asked; it never changes the widget, the state is changed by the bot
        await bot.AnswerWidgetQuery(widgetQueryId: context.Id, data: total.ToString(), cancellationToken: ct);
    }
}
```

* BotKit never answers a widget query: answer it within 15 seconds, otherwise the widget gets a timeout.
* `context.IsCopy` is true for a tap in a forwarded copy of a shared widget: such a query has no chat and no message.
* `context.QueryType` tells what the widget sent: plain data, data signed by a TON wallet, or a transaction notification; all of them are routed by the key.
* Needs Telegram.Bot 22.10.4 or newer (Bot API 10.4) and add `WidgetQuery` to `AllowedUpdates`.

A complete widget (HTML, sending, state, a fallback answer for unknown keys) is in the [Example documentation](Telegram.BotKit.Example/README.md).

---

## 📖 Examples

Handlers, middleware, state-based messages, inline queries, and the full configuration reference:

[Telegram.BotKit.Example Documentation](Telegram.BotKit.Example/README.md)
