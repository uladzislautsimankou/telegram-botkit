# Telegram.BotKit Example & Reference Guide

Usage examples and reference for routing, handlers, middleware pipelines, and conversational state.

---

## ⚙️ Configuration (`appsettings.json`)

```json
{
    "TelegramBot": {
        "Token": "YOUR_BOT_TOKEN_HERE",
        "Mode": "Polling",
        "AllowedUpdates": [ "Message", "CallbackQuery", "InlineQuery", "ChosenInlineResult" ],
        "WebhookUrl": "https://your-domain.com/api/bot",
        "SecretToken": "optional-secret-token",
        "DropPendingUpdates": false
    }
}
```

| Option | Meaning |
| --- | --- |
| `Token` | The bot token from [@BotFather](https://t.me/BotFather). Required. |
| `Mode` | `Polling` (default) or `Webhook`. |
| `AllowedUpdates` | The update types to receive (`Message`, `CallbackQuery`, ...). Omitted keeps the previous settings, an empty array resets to the defaults. Misspelled names fail at startup. |
| `WebhookUrl` | The public **HTTPS** address Telegram sends updates to; its path is the route of the endpoint. Webhook mode only. |
| `SecretToken` | Sent in the `X-Telegram-Bot-Api-Secret-Token` header; requests without it are rejected. Strongly recommended for webhooks. |
| `DropPendingUpdates` | Drops the updates that piled up while the bot was offline. |
| `DeleteWebhookOnShutdown` | Removes the webhook when the application stops. Off by default, so a stopping instance does not delete the webhook of a new one. |
| `MaxConnections`, `IpAddress`, `CertificatePath` | Webhook details: simultaneous connections (1-100), a fixed IP address for Telegram, a self-signed certificate. |
| `BaseUrl` | The address of a private Bot API server. |

The environment defaults to `Production` unless `DOTNET_ENVIRONMENT`, `ASPNETCORE_ENVIRONMENT` or `--environment` says otherwise. Keep the token out of the repository: use user secrets or the `TelegramBot__Token` environment variable.

---

## 🛠 Setup & Registration (`Program.cs`)

```csharp
using Telegram.Bot.Polling;
using Telegram.BotKit.Hosting;
using MyBot.Handlers;
using MyBot.Middlewares;

var builder = BotApplication.CreateBuilder(args);

builder.Services.AddCommands(pipeline =>
{
    pipeline.UseMiddleware<CommandErrorMiddleware>();          // before routing: catches the errors of everything below
    pipeline.UseRouting();
    pipeline.UseMiddleware<CommandRoutingResultMiddleware>();  // after routing: runs for unmatched commands too
});

builder.Services.AddCallbacks(pipeline =>
{
    pipeline.UseRouting();
    pipeline.UseMiddleware<CallbackRoutingResultMiddleware>();
});

builder.Services.AddInlineQueries(pipeline =>
{
    pipeline.UseRouting();
    pipeline.UseAnswer<LoggingInlineQueryAnswerMiddleware>();
});

builder.Services.AddChosenInlineResults();

builder.Services.AddWidgetQueries(pipeline =>
{
    pipeline.UseMiddleware<WidgetQueryFallbackAnswerMiddleware>();  // before routing: answers what was left unanswered
    pipeline.UseRouting();
});

builder.Services.AddMessages(pipeline =>
{
    pipeline.UseMiddleware<UserStateMiddleware>();
    pipeline.UseRouting();
});

builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();

var app = builder.Build();
await app.RunAsync();
```

Handlers are found automatically by scanning the calling assembly. To scan other assemblies pass them explicitly
(the first parameter is the pipeline callback, so use a named argument), or add a single handler by hand:

```csharp
// scan another assembly
builder.Services.AddCommands(assemblies: typeof(SharedCommands).Assembly);

// ...or add a single handler by hand (use one AddCommands call, not both)
builder.Services.AddCommands(pipeline => pipeline.AddHandler<PingCommandHandler>());
```

Call each `AddXxx()` once: scan several assemblies with a single call.

`Build()` checks the setup before the bot starts: an `IUpdateHandler` that can be created and valid parameter models of commands, callbacks and widget queries.

---

## 🚦 Update Dispatcher (`IUpdateHandler`)

Dispatches raw Telegram updates to framework dispatchers:

```csharp
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.WidgetQueries.Abstractions;

namespace MyBot.Handlers;

public sealed class BotUpdateHandler(
    ICommandDispatcher commandDispatcher,
    ICallbackDispatcher callbackDispatcher,
    IMessageDispatcher messageDispatcher,
    IInlineQueryDispatcher inlineQueryDispatcher,
    IChosenInlineResultDispatcher chosenInlineResultDispatcher,
    IWidgetQueryDispatcher widgetQueryDispatcher,
    ILogger<BotUpdateHandler> logger
) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        switch (update)
        {
            case { CallbackQuery: { } callback }:
                await callbackDispatcher.DispatchAsync(callback, cancellationToken);
                break;

            case { Message: { Text: string text } message } when text.StartsWith('/'):
                await commandDispatcher.DispatchAsync(message, cancellationToken);
                break;

            case { Message: { } message }:
                await messageDispatcher.DispatchAsync(message, cancellationToken);
                break;

            case { InlineQuery: { } inlineQuery }:
                await inlineQueryDispatcher.DispatchAsync(inlineQuery, cancellationToken);
                break;

            case { ChosenInlineResult: { } chosenResult }:
                await chosenInlineResultDispatcher.DispatchAsync(chosenResult, cancellationToken);
                break;

            case { WidgetQuery: { } widgetQuery }:
                await widgetQueryDispatcher.DispatchAsync(widgetQuery, cancellationToken);
                break;

            case { SharedWidgetQuery: { } sharedWidgetQuery }:
                await widgetQueryDispatcher.DispatchAsync(sharedWidgetQuery, cancellationToken);
                break;

            default:
                break;
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Update error from source: {Source}", source);
        return Task.CompletedTask;
    }
}
```

---

## 🚇 Middleware: Errors and Routing Results

The pipeline of every update kind is: middleware before `UseRouting()` → routing → middleware after `UseRouting()` → handler.

* Middleware **before** `UseRouting()` wraps everything after it: the place for error handling and for anything routing depends on (e.g. `MessageContext.UserState`).
* Middleware **after** `UseRouting()` runs also when **no handler matched**: it reads the routing result (`CommandContext.RouteResult`, `CallbackContext.MatchedCallbackMetadata`, `MessageContext.MatchedMessageMetadata`, ...). A middleware that does not call `next()` stops the pipeline.
* The framework never replies on its own: unknown commands, binding errors and failed handlers are answered by your middleware, otherwise they only reach `IUpdateHandler.HandleErrorAsync`.

### Command errors (before routing)

```csharp
public sealed class CommandErrorMiddleware(ITelegramBotClient bot, ILogger<CommandErrorMiddleware> logger) : ICommandMiddleware
{
    public async ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        catch (MissingParameterException ex)
        {
            await bot.SendMessage(context.Message.Chat.Id, $"Missing parameter: {ex.ParameterName}", cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Command failed");
            await bot.SendMessage(context.Message.Chat.Id, "Something went wrong.", cancellationToken: cancellationToken);
        }
    }
}
```

`ParameterBindingException` (base of `MissingParameterException` and `InvalidParameterTypeException`) and `InputParsingException` (e.g. an unclosed quote) are the exceptions of bad user input.

### Command routing result (after routing)

```csharp
public sealed class CommandRoutingResultMiddleware(ITelegramBotClient bot) : ICommandMiddleware
{
    public async ValueTask InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        var chatId = context.Message.Chat.Id;

        switch (context.RouteResult.Status)
        {
            case CommandRouteStatus.Success:
                await next();                                   // a handler is matched
                break;

            case CommandRouteStatus.Suggestions:                // a typo: similar commands are suggested
                var list = string.Join(", ", context.RouteResult.Suggestions.Select(c => $"/{c}"));
                await bot.SendMessage(chatId, $"Unknown command. Did you mean: {list}?", cancellationToken: cancellationToken);
                break;

            case CommandRouteStatus.NotFound:
                await bot.SendMessage(chatId, "Unknown command.", cancellationToken: cancellationToken);
                break;

            // Ignored: addressed to another bot or not meant for us, VirtualRouteNotFound: log it
        }
    }
}
```

To ignore a command completely, skip `next()` for it in a middleware placed before `UseRouting()`.

### Callback routing result (after routing)

```csharp
public sealed class CallbackRoutingResultMiddleware(ITelegramBotClient bot) : ICallbackMiddleware
{
    public async ValueTask InvokeAsync(CallbackContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        if (context.MatchedCallbackMetadata is not null)
        {
            await next();                                       // a handler is matched
            return;
        }

        // e.g. a button of a message sent before a deployment removed its handler:
        // answer it, otherwise the button keeps showing the loading indicator
        await bot.AnswerCallbackQuery(context.Query.Id, "This button is no longer active.", showAlert: true, cancellationToken: cancellationToken);
    }
}
```

### Widget query fallback answer (before routing)

A widget that is not answered waits for 15 seconds and gets a timeout. A middleware before `UseRouting()` sees the end of the whole pipeline, so one place covers both a key nobody handles and a handler that failed:

```csharp
public sealed class WidgetQueryFallbackAnswerMiddleware(ITelegramBotClient bot, ILogger<WidgetQueryFallbackAnswerMiddleware> logger) : IWidgetQueryMiddleware
{
    public async ValueTask InvokeAsync(WidgetQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();

            if (context.MatchedWidgetQueryMetadata is not null)
            {
                return;                                         // a handler ran and answered
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Widget query '{Key}' failed", context.Key);
        }

        // no handler for the key (e.g. a widget sent before a deployment removed it) or the handler failed:
        // an empty answer ends the wait
        await bot.AnswerWidgetQuery(widgetQueryId: context.Id, cancellationToken: cancellationToken);
    }
}
```

### Inline query errors (before routing)

A failing inline handler does not throw: its exception is logged and collected in `InlineQueryContext.Exceptions`, and a query that no handler matched is answered with an empty list. A middleware before `UseRouting()` only has to catch what is left, e.g. a failed answer:

```csharp
public sealed class InlineQueryErrorMiddleware(ILogger<InlineQueryErrorMiddleware> logger) : IInlineQueryMiddleware
{
    public async ValueTask InvokeAsync(InlineQueryContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Inline query {QueryId} failed", context.Query.Id);
        }
    }
}
```

---

## 💬 Commands (`ICommandHandler`)

### 1. Parameter Model

```csharp
using Telegram.BotKit.Commands.Attributes;

namespace MyBot.Models;

public sealed record PingCommandParams
{
    // positional argument: /ping "hello world"
    [CommandParam(Position = 0, Required = true)]
    public string? Message { get; init; }

    // named argument: /ping c=5
    [CommandParam(Name = "c")]
    public int? Count { get; init; }
}
```

### 2. Command Handler

```csharp
using Telegram.Bot;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using MyBot.Models;

namespace MyBot.Handlers;

public sealed class PingCommandHandler(ITelegramBotClient bot) : ICommandHandler<PingCommandParams>
{
    public static string Command => "ping";

    public static string[] Aliases => ["p", "check"];

    public async Task HandleAsync(PingCommandParams parameters, CommandContext context, CancellationToken cancellationToken)
    {
        await bot.SendMessage(
            chatId: context.Message.Chat.Id,
            text: $"Pong! Message: {parameters.Message}, Count: {parameters.Count ?? 0}",
            replyParameters: context.Message.Id,
            cancellationToken: cancellationToken
        );
    }
}
```

> For parameterless commands use `ICommandHandler<EmptyCommandParams>`.

---

## 🔘 Inline Callbacks (`ICallbackHandler`)

Maps query parameters from `callback_data` (e.g. `example:ping?m=test&c=1`).

### 1. Parameter Model

```csharp
using Telegram.BotKit.Callbacks.Attributes;

namespace MyBot.Models;

public sealed record PingCallbackParams
{
    // short names save space: callback_data is limited to 64 bytes
    [CallbackParam(Name = "m")]
    public string? Message { get; init; }

    [CallbackParam(Name = "c")]
    public int? Count { get; init; }
}
```

### 2. Callback Handler

```csharp
using Telegram.Bot;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;
using MyBot.Models;

namespace MyBot.Handlers;

public sealed class PingCallbackHandler(ITelegramBotClient bot) : ICallbackHandler<PingCallbackParams>
{
    public static string Key => "example:ping";

    public async Task HandleAsync(PingCallbackParams parameters, CallbackContext context, CancellationToken cancellationToken)
    {
        await bot.AnswerCallbackQuery(
            callbackQueryId: context.Query.Id,
            text: $"Callback received: {parameters.Message}",
            showAlert: true,
            cancellationToken: cancellationToken
        );
    }
}
```

> For buttons without parameters use `ICallbackHandler<EmptyCallbackParams>`.

### 3. Building the Button

The callback data of a button is built from the same types, so the key and the parameter names cannot drift apart:

```csharp
using Telegram.BotKit.Callbacks.Building;

var button = CallbackButton.Create<PingCallbackHandler, PingCallbackParams>(
    "Ping",
    new PingCallbackParams { Message = "hello", Count = 1 });
```

---

## 🔍 Inline Queries (`IInlineQueryHandler`)

```csharp
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;

namespace MyBot.Handlers;

public sealed class SearchInlineQueryHandler : IInlineQueryHandler
{
    public static bool CanHandle(InlineQuery query) =>
        query.Query.StartsWith("search", StringComparison.OrdinalIgnoreCase);

    // optional: handlers with a lower order come first in the answer
    public static int Order => 0;

    public Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken)
    {
        var keyword = context.Query.Query["search".Length..].Trim();

        var results = new InlineQueryResult[]
        {
            new InlineQueryResultArticle(
                id: "1",
                title: $"Result: {keyword}",
                inputMessageContent: new InputTextMessageContent($"Selected: {keyword}")
            )
        };

        return Task.FromResult(new InlineQueryBatch(results, isPersonal: true, cacheTime: 300));
    }
}
```

### Answer Middleware (`IInlineQueryAnswerMiddleware`)

Intercepts or inspects results before sending to Telegram:

```csharp
using Telegram.Bot;
using Telegram.BotKit.InlineQueries.Middlewares;
using Telegram.BotKit.InlineQueries.Models;

namespace MyBot.Middlewares;

public sealed class LoggingInlineQueryAnswerMiddleware(
    ITelegramBotClient botClient,
    ILogger<LoggingInlineQueryAnswerMiddleware> logger
) : DefaultInlineQueryAnswerMiddleware(botClient, logger)
{
    protected override async Task SendAnswerAsync(InlineQueryContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("Answering query {QueryId} with {Count} results",
            context.Query.Id, context.Results.Count);

        await base.SendAnswerAsync(context, cancellationToken);
    }
}
```

---

## 🎯 Chosen Inline Results (`IChosenInlineResultHandler`)

Evaluates `CanHandle` statically before instantiating the handler from DI:

```csharp
using Telegram.Bot.Types;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Models;

namespace MyBot.Handlers;

public sealed class ArticleChosenInlineResultHandler(
    ILogger<ArticleChosenInlineResultHandler> logger
) : IChosenInlineResultHandler
{
    public static bool CanHandle(ChosenInlineResult chosenResult) =>
        chosenResult.ResultId.StartsWith("article_", StringComparison.OrdinalIgnoreCase);

    public Task HandleAsync(ChosenInlineResultContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {UserId} selected {ResultId}",
            context.Result.From.Id,
            context.Result.ResultId);

        return Task.CompletedTask;
    }
}
```

---

## 🧩 Widget Queries (`IWidgetQueryHandler`)

An HTML widget (sent with `sendWidget`) asks the bot with `TelegramWidget.request(data)`: a button inside the widget that works like a callback button. The query arrives as `widget_query` (a tap in the original message) or `shared_widget_query` (a tap in a forwarded copy of a shared widget) and is routed by the key of its `data`, which has the format of callback data: `counter:add?by=5`, 1-64 bytes.

> Needs Telegram.Bot with Bot API 10.4 (22.10.4) and a client that supports widgets (layer 233). Add `WidgetQuery` to `AllowedUpdates` (and `SharedWidgetQuery` if you send shared widgets).

### 1. The widget

One self-contained HTML document: inline CSS and JS, no network, at most 32 KB, and at most 4 KB after zlib (level 9). The counter of the example (`Widgets/counter.html`, about 1 KB packed) draws the number from its state and asks the bot to change it:

```html
<script>
const W = TelegramWidget;

// the chat state is written only by the bot and reaches the widget as an event
show(parse(W.chatData));
W.onEvent('chatDataChanged', () => show(parse(W.chatData)));

// a request has to be started by a tap, so this runs in the click handler
document.getElementById('five').addEventListener('click', () => {
  W.request('counter:add?by=5').then(
    (answer) => show(parse(answer.result)),       // the answer carries the new state at once
    (error) => showError(error.code));            // NOT_ALLOWED, BUSY, RATE_LIMITED, TIMEOUT, ...
});
</script>
```

The HTML never changes after sending: a new layout is a new message. What changes is the state, `chat_data` (this message, never forwarded) and `public_data` (shared with forwards), up to 1024 bytes each, written only by the bot.

### 2. Parameter Model and Handler

```csharp
using Telegram.Bot;
using Telegram.BotKit.WidgetQueries.Abstractions;
using Telegram.BotKit.WidgetQueries.Attributes;
using Telegram.BotKit.WidgetQueries.Models;

namespace MyBot.Handlers;

public sealed record CounterParams
{
    // short names save space: the data is limited to 64 bytes
    [WidgetParam(Name = "by")]
    public int By { get; init; } = 1;
}

public sealed class CounterWidgetQueryHandler(ITelegramBotClient bot, CounterStore store) : IWidgetQueryHandler<CounterParams>
{
    public static string Key => "counter:add";

    public async Task HandleAsync(CounterParams parameters, WidgetQueryContext context, CancellationToken cancellationToken)
    {
        // a copy and a message sent via the bot have no chat and message id
        if (context.Chat is not { } chat || context.MessageId is not { } messageId)
        {
            await bot.AnswerWidgetQuery(widgetQueryId: context.Id, cancellationToken: cancellationToken);
            return;
        }

        var state = CounterStore.ToState(store.Add(chat.Id, messageId, Math.Clamp(parameters.By, 1, 5)));

        // the answer is text for the widget that asked, it never changes the widget
        await bot.AnswerWidgetQuery(widgetQueryId: context.Id, data: state, cancellationToken: cancellationToken);

        // the state of the message is changed by the bot
        await bot.EditMessageWidgetData(chatId: chat.Id, messageId: messageId, chatData: state, cancellationToken: cancellationToken);
    }
}
```

> For queries without parameters use `IWidgetQueryHandler<EmptyWidgetQueryParams>`.

### 3. Sending the Widget

```csharp
await bot.SendWidget(
    chatId: chat.Id,
    html: html,                                // read once, e.g. with WidgetHtml.Load("counter.html")
    allowQueries: true,                        // without it Telegram refuses every request of the widget
    chatData: CounterStore.ToState(0),         // the state the widget draws first
    cancellationToken: cancellationToken);
```

Things to know:

* The framework never answers a widget query: answer it within 15 seconds (`AnswerWidgetQuery`, text of up to 1024 bytes), otherwise the widget gets a timeout. See the fallback middleware above.
* Telegram does not tell the bot what the widget shows: keep what you need per message (`CounterStore`), or write it to the state with `EditMessageWidgetData`. An edit that changes nothing fails with `message is not modified`; take it as done.
* `context.IsCopy` is true for a tap in a forwarded copy: `Chat`, `MessageId` and `InlineMessageId` are null then, and only `From`, `Data` and `WidgetId` are known. `(WidgetId, From.Id)` is safe for counting "one vote per user".
* `context.QueryType` tells what the widget sent: plain data, data signed by a TON wallet, or a notification about a transaction. All of them are routed by the key.
* A user can send at most 5 queries per 5 seconds to the bot. The data comes from the widget: treat it as user input.

---

## ✉️ Message Handlers & Conversational States

```
[ User sends "/setage" ] ──> Command sets state "awaiting_age"
                                       │
[ User sends "25" ]      ──> UserStateMiddleware sets context.UserState = "awaiting_age"
                                       │
                             Router executes handler with RequiredState == "awaiting_age"
```

### 1. State Storage Abstraction

```csharp
namespace MyBot.Services;

public interface IStateStorage
{
    Task<string?> GetStateAsync(long userId);
    Task SetStateAsync(long userId, string? state);
}
```

### 2. State Middleware

```csharp
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared.Pipeline;
using MyBot.Services;

namespace MyBot.Middlewares;

public sealed class UserStateMiddleware(IStateStorage stateStorage) : IMessageMiddleware
{
    public async ValueTask InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        var userId = context.Message.From?.Id;
        if (userId is not null)
        {
            context.UserState = await stateStorage.GetStateAsync(userId.Value);
        }

        await next();
    }
}
```

### 3. Setting State in a Command

```csharp
using Telegram.Bot;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;
using MyBot.Services;

namespace MyBot.Handlers;

public sealed class SetAgeCommandHandler(
    IStateStorage stateStorage,
    ITelegramBotClient bot
) : ICommandHandler<EmptyCommandParams>
{
    public static string Command => "setage";

    public async Task HandleAsync(EmptyCommandParams parameters, CommandContext context, CancellationToken cancellationToken)
    {
        var userId = context.Message.From!.Id;
        await stateStorage.SetStateAsync(userId, "awaiting_age");

        await bot.SendMessage(
            chatId: context.Message.Chat.Id,
            text: "Please enter your age:",
            replyParameters: context.Message.Id,
            cancellationToken: cancellationToken
        );
    }
}
```

### 4. State-Bound Message Handler

```csharp
using Telegram.Bot;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using MyBot.Services;

namespace MyBot.Handlers;

public sealed class AgeInputMessageHandler(
    IStateStorage stateStorage,
    ITelegramBotClient bot
) : IMessageHandler
{
    public static string? RequiredState => "awaiting_age";

    public static bool CanHandle(MessageContext context) =>
        !string.IsNullOrWhiteSpace(context.Message.Text);

    public async Task HandleAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var input = context.Message.Text!;
        var userId = context.Message.From!.Id;

        if (int.TryParse(input, out var age))
        {
            await stateStorage.SetStateAsync(userId, null);
            await bot.SendMessage(userId, $"Age saved: {age}!", cancellationToken: cancellationToken);
            return;
        }

        await bot.SendMessage(userId, "Please enter a valid number:", cancellationToken: cancellationToken);
    }
}
```

### 5. Global Message Handler (Fallback)

Executes when no state-bound handler matches:

```csharp
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;

namespace MyBot.Handlers;

public sealed class EchoStickerMessageHandler(ITelegramBotClient bot) : IMessageHandler
{
    public static string? RequiredState => null;

    public static bool CanHandle(MessageContext context) =>
        context.Message is { Chat.Type: ChatType.Private, Sticker: not null };

    public async Task HandleAsync(MessageContext context, CancellationToken cancellationToken)
    {
        await bot.SendSticker(
            chatId: context.Message.Chat.Id,
            sticker: InputFile.FromFileId(context.Message.Sticker!.FileId),
            cancellationToken: cancellationToken
        );
    }
}
```
