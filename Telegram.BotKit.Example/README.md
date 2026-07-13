# Telegram.BotKit Developer Manual & Reference 🚀

A comprehensive guide demonstrating the capabilities, routing mechanics, parameter-binding, and stateful dialog architecture of the framework.

---

## ▶️ How to Run the Example Project

The example project comes pre-configured with sample handlers and pipelines.

1. Open `appsettings.json`.
2. Insert your bot token inside the `TelegramBot` configuration block:
   ```json
   {
     "TelegramBot": {
       "Token": "YOUR_BOT_TOKEN_HERE"
     }
   }
   ```
3. Run the project from your terminal:
   ```bash
   dotnet run
   ```

---

## 🛠 Unified Registration & Routing (`Program.cs`)

This is what the application's entry point looks like. You **must** register an implementation of `IUpdateHandler` (provided by the `Telegram.Bot` library) in your DI container. This handler is injected into the background services to dispatch raw updates into the framework's specific pipelines.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Polling;
using Telegram.BotKit.Hosting;
using MyBot.Handlers;

var builder = BotApplication.CreateBuilder(args);

// 1. Register Pipelines (automatically scans the calling assembly for handlers)
builder.Services
    .AddCommands()
    .AddCallbacks()
    .AddMessages()
    .AddInlineQueries();

// 2. Register your custom Update Handler (Crucial for routing raw updates to dispatchers)
builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();

var app = builder.Build();
await app.RunAsync();
```

### Reference: Minimal `BotUpdateHandler` Implementation

Your update handler acts as a traffic router. It determines which dispatcher should process the incoming update based on its type.

```csharp
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.Messages.Abstractions;

namespace MyBot.Handlers;

public sealed class BotUpdateHandler(
    ICommandDispatcher commandDispatcher,
    ICallbackDispatcher callbackDispatcher,
    IMessageDispatcher messageDispatcher,
    IInlineQueryDispatcher inlineQueryDispatcher
    ) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is { } message)
        {
            if (message.Text is { } text && text.StartsWith('/'))
            {
                await commandDispatcher.DispatchAsync(message, cancellationToken);
            }
            else
            {
                await messageDispatcher.DispatchAsync(message, cancellationToken);
            }
        }
        else if (update.CallbackQuery is { } callbackQuery)
        {
            await callbackDispatcher.DispatchAsync(callbackQuery, cancellationToken);
        }
        else if (update.InlineQuery is { } inlineQuery)
        {
            await inlineQueryDispatcher.DispatchAsync(inlineQuery, cancellationToken);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        // Handle global network or API errors here
        return Task.CompletedTask;
    }
}
```

---

## 💬 Creating a Command (`ICommandHandler`)

Configure a parameter model and declare your command trigger.

### 1. Define the DTO (Parameters)

Describe the positional or named arguments your command accepts.

```csharp
using Telegram.BotKit.Commands.Attributes;

public record PingCommandParams
{
    // Required positional argument (index 0). Example: /ping "Hello"
    [CommandParam(Position = 0, Required = true)] 
    public string? Message { get; init; } 

    // Optional named argument. Example: /ping c=5
    [CommandParam(Name = "c")]
    public int? Count { get; init; }
}
```

### 2. Create the Handler

Implement the handler logic using `ICommandHandler<TParams>`.

```csharp
using Telegram.Bot;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;

public class PingCommandHandler(ITelegramBotClient bot) : ICommandHandler<PingCommandParams>
{
    public string Command => "ping";

    public async Task HandleAsync(PingCommandParams parameters, CommandContext context, CancellationToken cancellationToken)
    {
        await bot.SendMessage(
            chatId: context.Message.Chat.Id,
            text: $"Pong! Message: {parameters.Message}, Count: {parameters.Count ?? 0}",
            cancellationToken: cancellationToken
        );
    }
}
```

---

## 🔘 Creating a Callback (`ICallbackHandler`)

Map query parameters from inline keyboards safely and transparently into strongly-typed handlers.

### 1. Define the DTO

Use standard JSON attribute overrides to keep payload keys compact inside Telegram's callback byte-limit constraint.

```csharp
using System.Text.Json.Serialization;

public record PingCallbackParams
{
    [JsonPropertyName("m")] 
    public string? Message { get; init; }

    [JsonPropertyName("c")]
    public int? Count { get; init; }
}
```

### 2. Create the Handler

Implement the handler logic using `ICallbackHandler<TParams>`.

```csharp
using Telegram.Bot;
using Telegram.BotKit.Callbacks.Abstractions;
using Telegram.BotKit.Callbacks.Models;

public class PingCallbackHandler(ITelegramBotClient bot) : ICallbackHandler<PingCallbackParams>
{
    public string Key => "example:ping"; // Matches callback data format like example:ping?m=text&c=5

    public async Task HandleAsync(PingCallbackParams parameters, CallbackContext context, CancellationToken cancellationToken)
    {
        await bot.AnswerCallbackQuery(
            callbackQueryId: context.Query.Id,
            text: $"Callback received! Msg: {parameters.Message}",
            showAlert: true,
            cancellationToken: cancellationToken
        );
    }
}
```

---

## 🔍 Creating an Inline Query (`IInlineQueryHandler`)

Respond to structured inline queries entered by users inside any conversation thread. Match and process queries dynamically inside the `CanHandle` block.

```csharp
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.BotKit.InlineQueries.Abstractions;
using Telegram.BotKit.InlineQueries.Models;

public class SearchInlineQueryHandler : IInlineQueryHandler
{
    // Match queries starting with "search" (e.g. "@YourBot search keyword")
    public bool CanHandle(InlineQuery query) =>
        query.Query.StartsWith("search", StringComparison.OrdinalIgnoreCase);

    public async Task<InlineQueryBatch> HandleAsync(InlineQueryContext context, CancellationToken cancellationToken)
    {
        var searchText = context.Query.Query["search".Length..].Trim();

        var results = new InlineQueryResult[]
        {
            new InlineQueryResultArticle(
                id: "1",
                title: $"Result: {searchText}",
                inputMessageContent: new InputTextMessageContent($"Selected search keyword: {searchText}")
            )
        };

        return new InlineQueryBatch(results, IsPersonal: true, CacheTime: 300);
    }
}
```

---

## ✉️ Creating Message Handlers (`IMessageHandler`)

Message handlers process plain text, media, or service events. They can be registered as global handlers or bound to a specific conversational state.

### 1. Global Message Handler

Global handlers have no state constraints (`RequiredState => null`) and process messages matching custom predicate criteria.

```csharp
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;

public sealed class SimpleStickerHandler(ITelegramBotClient bot) : IMessageHandler
{
    // Runs regardless of the active user state
    public string? RequiredState => null;

    // Restricts handling to sticker messages sent in private chats
    public bool CanHandle(MessageContext context) =>
        context.Message is { Chat.Type: ChatType.Private, Sticker: { } };

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

### 2. State-Bound Message Handler

Stateful handlers only execute if the framework's state-middleware resolves a user state matching `RequiredState`.

```csharp
using Telegram.Bot;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;

public sealed class AgeInputHandler(IStateStorage stateStorage, ITelegramBotClient bot) : IMessageHandler
{
    // Active only if the state middleware resolved user state as "awaiting_age"
    public string? RequiredState => "awaiting_age";

    public bool CanHandle(MessageContext context) =>
        !string.IsNullOrWhiteSpace(context.Message.Text);

    public async Task HandleAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var input = context.Message.Text!;
        var userId = context.Message.From!.Id;

        if (int.TryParse(input, out var age))
        {
            // Save valid input and clear the conversational state
            await stateStorage.SetStateAsync(userId, null);
            await bot.SendMessage(userId, $"Age saved: {age}!", cancellationToken: cancellationToken);
        }
        else
        {
            // Validation failed: keep state active and prompt again
            await bot.SendMessage(userId, "Please enter a valid number:", cancellationToken: cancellationToken);
        }
    }
}
```

---

## 🔄 Stateful Dialogs (FSM / Wizard Flow Setup)

A common bot pattern: a user triggers a flow (e.g., types `/setage`), the system expects a text answer next, and we route that specific text to a designated state-bound handler.

```
[ User sends "/setage" ] ──> CommandHandler updates State to "awaiting_age" in Cache/DB
                                        │
[ User sends "25" ]      ──> StateMiddleware loads "awaiting_age" -> Assigns to context.UserState
                                        │
                             Router matches handler with RequiredState == "awaiting_age"
```

### Step 1: Create State Storage Abstraction
```csharp
public interface IStateStorage
{
    Task<string?> GetStateAsync(long userId);
    Task SetStateAsync(long userId, string? state);
}
```

### Step 2: Implement State Resolving Middleware
Before routing standard messages, this middleware fetches the current state of the sender and assigns it to `context.UserState`.

```csharp
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;
using Telegram.BotKit.Shared;

namespace MyBot.Middlewares;

public sealed class UserStateMiddleware(IStateStorage stateStorage) : IMessageStateMiddleware
{
    public async Task InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
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

### Step 3: Trigger Conversation State in Command
```csharp
using Telegram.Bot;
using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Models;

public sealed class SetAgeCommandHandler(IStateStorage stateStorage, ITelegramBotClient bot) : ICommandHandler<NoParams>
{
    public string Command => "setage";
    public string[] Aliases => [];

    public async Task HandleAsync(NoParams parameters, CommandContext context, CancellationToken cancellationToken)
    {
        var userId = context.Message.From!.Id;

        // Set state to trigger the stateful handler on the next user text update
        await stateStorage.SetStateAsync(userId, "awaiting_age");

        await bot.SendMessage(
            chatId: context.Message.Chat.Id,
            text: "Please enter your age:",
            cancellationToken: cancellationToken
        );
    }
}
```

### Step 4: Handle State Input and Reset (FSM Exit)
Implement the message handler with `RequiredState => "awaiting_age"` (as shown in the **Message Handlers** section above) to validate the input, save it, and then call `await stateStorage.SetStateAsync(userId, null)` to exit the state flow.