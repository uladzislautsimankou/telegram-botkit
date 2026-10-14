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

> **ChosenInlineResult:** Telegram disables `chosen_inline_result` updates by default. Enable via [@BotFather](https://t.me/BotFather):
> `/mybots` -> select bot -> **Bot Settings** -> **Inline Feedback** -> set to `100%`.

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
    pipeline.UseMiddleware<CommandLoggingMiddleware>();
    pipeline.UseRouting();
});

builder.Services.AddCallbacks();

builder.Services.AddInlineQueries(pipeline =>
{
    pipeline.UseRouting();
    pipeline.UseAnswer<LoggingInlineQueryAnswerMiddleware>();
});

builder.Services.AddChosenInlineResults();

builder.Services.AddMessages(pipeline =>
{
    pipeline.UseMiddleware<UserStateMiddleware>();
    pipeline.UseRouting();
});

builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();

var app = builder.Build();
await app.RunAsync();
```

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

namespace MyBot.Handlers;

public sealed class BotUpdateHandler(
    ICommandDispatcher commandDispatcher,
    ICallbackDispatcher callbackDispatcher,
    IMessageDispatcher messageDispatcher,
    IInlineQueryDispatcher inlineQueryDispatcher,
    IChosenInlineResultDispatcher chosenInlineResultDispatcher,
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
using System.Text.Json.Serialization;

namespace MyBot.Models;

public sealed record PingCallbackParams
{
    [JsonPropertyName("m")]
    public string? Message { get; init; }

    [JsonPropertyName("c")]
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
    public bool CanHandle(InlineQuery query) =>
        query.Query.StartsWith("search", StringComparison.OrdinalIgnoreCase);

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
using Telegram.BotKit.Shared;
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