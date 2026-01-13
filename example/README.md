# Telegram.BotKit Example

A console application demonstrating the capabilities of the framework.

## 📦 Installation

To use the framework in your own project, install the package:

```bash
dotnet add package Telegram.BotKit
```

## ▶️ How to Run
1. Open `appsettings.json`.
2. Insert your bot token into the `Token` field.
3. Run the project.

---

## 1. Creating a Command

1. **Define the DTO (Parameters):**
Describe the arguments your command accepts.

```csharp
public record PingCommandParams
{
    // Positional argument 0. Example: /ping "Hello"
    [CommandParam(Position = 0)] 
    public string Message { get; init; } 
}
```

2. **Create the Handler:**
Implement the logic.

```csharp
public class PingCommandHandler(ITelegramBotClient bot) : ICommandHandler<PingCommandParams>
{
    public string Command => "ping"; // Trigger command

    public async Task HandleAsync(PingCommandParams args, CommandContext ctx, CancellationToken ct)
    {
        await bot.ReplyMessage(ctx, $"Pong! You said: {args.Message}");
    }
}
```

## 2. Creating a Callback (Inline Button)

1. **Define the DTO:**
Use `JsonPropertyName` to keep keys short in the URL.

```csharp
public record VoteCallbackParams
{
    [JsonPropertyName("id")] 
    public int PollId { get; init; }
}
```

2. **Create the Handler:**

```csharp
public class VoteCallbackHandler : ICallbackHandler<VoteCallbackParams>
{
    public string Key => "vote"; // Example data: vote?id=5

    public async Task HandleAsync(VoteCallbackParams args, CallbackContext ctx, CancellationToken ct)
    {
        // Your logic here...
    }
}
```

## 3. Registration (Program.cs)

Use the `BotBuilder` to set up the application and register all handlers automatically.

```csharp
var builder = BotApplication.CreateBuilder(args);

// Automatically finds and registers all handlers in the current assembly
builder.Services.AddCommandHandlers(typeof(Program).Assembly);
builder.Services.AddCallbackHandlers(typeof(Program).Assembly);

var app = builder.Build();
await app.RunAsync();
```