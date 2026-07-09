# Telegram.BotKit Example

A console reference application demonstrating the capabilities, routing mechanics, and parameter-binding architecture of the framework.

## 📦 Installation

To use the framework in your own project, install the NuGet package:

```bash
dotnet add package Telegram.BotKit
```

## ▶️ How to Run

*   Open `appsettings.json`.
*   Insert your bot token inside the `TelegramBot` configuration block:
    ```json
    {
      "TelegramBot": {
        "Token": "YOUR_BOT_TOKEN_HERE"
      }
    }
    ```
*   Ensure that the `appsettings.json` file is configured to copy to the output directory (Copy if Newer).
*   Run the project.

---

## Creating a Command

Configure a parameter model and declare your command trigger.

### Define the DTO (Parameters)

Describe the positional or named arguments your command accepts.

```csharp
public record PingCommandParams
{
    // required positional argument (index 0). Example: /ping "Hello"
    [CommandParam(Position = 0, Required = true)] 
    public string? Message { get; init; } 

    // optional named argument. Example: /ping c=5
    [CommandParam(Name = "c")]
    public int? Count { get; init; }
}
```

### Create the Handler

Implement the handler logic using `ICommandHandler<TParams>`.

```csharp
public class PingCommandHandler(ITelegramBotClient bot) : ICommandHandler<PingCommandParams>
{
    public string Command => "ping";

    public async Task HandleAsync(PingCommandParams parameters, CommandContext context, CancellationToken cancellationToken)
    {
        await bot.ReplyMessage(context, $"Pong! Message: {parameters.Message}, Count: {parameters.Count ?? 0}");
    }
}
```

---

## Creating a Callback (Inline Keyboard Button)

Map query parameters from inline keyboards safely and transparently into strongly-typed handlers.

### Define the DTO

Use `JsonPropertyName` overrides to keep payload keys compact inside Telegram's callback byte-limit constraint.

```csharp
public record PingCallbackParams
{
    [JsonPropertyName("m")] 
    public string? Message { get; init; }

    [JsonPropertyName("c")]
    public int? Count { get; init; }
}
```

### Create the Handler

Implement the handler logic using `ICallbackHandler<TParams>`.

```csharp
public class PingCallbackHandler(ITelegramBotClient bot) : ICallbackHandler<PingCallbackParams>
{
    public string Key => "example:ping"; // matches callback data format like example:ping?m=text&c=5

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

## Creating an Inline Query

Respond to structured inline queries entered by users inside any conversation thread.

### Define the DTO

Describe the search keyword parameter and any optional configurations.

```csharp
public record SearchQueryParams
{
    // required query text. Example: @YourBot search query_text
    [InlineQyeryParam(Position = 0, Required = true)]
    public string Query { get; init; } = string.Empty;

    // optional named parameter. Example: @YourBot search query_text limit=10
    [InlineQyeryParam(Name = "limit")]
    public int Limit { get; init; } = 5;
}
```

### Create the Handler

Implement the handler logic using `IInlineQueryHandler<TParams>`.

```csharp
public class SearchInlineQueryHandler(ITelegramBotClient bot) : IInlineQueryHandler<SearchQueryParams>
{
    public string Key => "search";

    public async Task HandleAsync(SearchQueryParams parameters, InlineQueryContext context, CancellationToken cancellationToken)
    {
        var results = new InlineQueryResult[]
        {
            new InlineQueryResultArticle(
                id: "1",
                title: $"Result: {parameters.Query}",
                inputMessageContent: new InputTextMessageContent($"Selected: {parameters.Query} (Max limit: {parameters.Limit})")
            )
        };

        await bot.AnswerInlineQuery(context.Id, results, cancellationToken: cancellationToken);
    }
}
```

---

## Creating a Global Catch-All Inline Query

If you want to capture raw query text directly (without requiring a specific trigger prefix like `search`), declare a handler with an empty key (`Key => string.Empty`). This is ideal for dynamic real-time suggestions like parsing promo codes as the user types.

### Define the DTO

Use a simple parameter model to capture raw text.

```csharp
public record RawQueryParams
{
    public string? RawText { get; init; }
}
```

### Create the Handler

Implement the fallback handler by returning an empty string for the key.

```csharp
public class InteractiveInlineQueryHandler(ITelegramBotClient bot) : IInlineQueryHandler<RawQueryParams>
{
    // empty key captures all raw input that doesn't match specific query keys
    public string Key => string.Empty;

    public async Task HandleAsync(RawQueryParams parameters, InlineQueryContext context, CancellationToken cancellationToken)
    {
        var input = context.Query?.Trim() ?? string.Empty;
        var results = new List<InlineQueryResult>();

        var promoCodeText = string.IsNullOrWhiteSpace(input) ? "promocode" : input;
        results.Add(new InlineQueryResultArticle(
            id: "promo",
            title: "Enter Promo Code",
            inputMessageContent: new InputTextMessageContent($"Promo code applied: {promoCodeText}")
        ));

        // conditionally show another result if input can be parsed as a number
        var isNumeric = int.TryParse(input, out var number);
        if (string.IsNullOrWhiteSpace(input) || isNumeric)
        {
            results.Add(new InlineQueryResultArticle(
                id: "add_number",
                title: "Add Number",
                inputMessageContent: new InputTextMessageContent($"Number added: {(isNumeric ? number : 0)}")
            ));
        }

        await bot.AnswerInlineQuery(context.Id, results, cancellationToken: cancellationToken);
    }
}
```

---

## Creating Message Handlers

Message handlers process plain text, media, or service events. They can be registered as global handlers or bound to a specific conversational state.

### 1. Global Message Handler

Global handlers have no state constraints (`RequiredState => null`) and process messages matching custom predicate criteria.

```csharp
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.BotKit.Abstractions;

namespace MyBot.Handlers;

public sealed class SimpleStickerHandler(ITelegramBotClient bot) : IMessageHandler
{
    // runs regardless of the active user state
    public string? RequiredState => null;

    // restricts handling to sticker messages sent in private chats
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
using Telegram.BotKit;
using Telegram.BotKit.Abstractions;

namespace MyBot.Handlers;

public sealed class AgeInputHandler(IStateStorage stateStorage) : IMessageHandler
{
    // active only if the state middleware resolved user state as "awaiting_age"
    public string? RequiredState => "awaiting_age";

    public bool CanHandle(MessageContext context) =>
        !string.IsNullOrWhiteSpace(context.Message.Text);

    public async Task HandleAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var input = context.Message.Text!;
        var userId = context.Message.From!.Id;

        if (int.TryParse(input, out var age))
        {
            // save the valid input and clear the conversational state
            await stateStorage.SetStateAsync(userId, null);
            await context.Message.ReplyAsync($"Age saved: {age}!", cancellationToken);
        }
        else
        {
            // validation failed: keep the state active and prompt again
            await context.Message.ReplyAsync("Please enter a valid number:", cancellationToken);
        }
    }
}
```

---

## Registration (Program.cs)

Use the host builder to establish the application container and automatically scan the assembly for all handlers.

```csharp
var builder = BotApplication.CreateBuilder(args);

// automatically registers command, callback, message, and inline query handlers
var assembly = typeof(Program).Assembly;
builder.Services.AddCommandHandlers(assembly);
builder.Services.AddCallbackHandlers(assembly);
builder.Services.AddMessageHandlers(assembly);
builder.Services.AddInlineQueryHandlers(assembly);

var app = builder.Build();
await app.RunAsync();
```