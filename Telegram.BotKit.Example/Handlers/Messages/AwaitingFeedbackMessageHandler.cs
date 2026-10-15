using Telegram.Bot;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Models;

namespace Telegram.BotKit.Example.Handlers.Messages;

/// <summary>
/// Processes messages targeted to the awaiting_feedback user state.
/// </summary>
internal sealed class AwaitingFeedbackMessageHandler(ITelegramBotClient bot) : IMessageHandler
{
    public static string? RequiredState => "awaiting_feedback";

    public static bool CanHandle(MessageContext context) =>
        context.Message.Text is not null;

    public async Task HandleAsync(MessageContext context, CancellationToken cancellationToken)
    {
        var content = context.Message.Text!["feedback:".Length..].Trim();

        await bot.SendMessage(
            chatId: context.Message.Chat.Id,
            text: $"Feedback received: \"{content}\"",
            replyParameters: context.Message.Id,
            cancellationToken: cancellationToken);
    }
}
