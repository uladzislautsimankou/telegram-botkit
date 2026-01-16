using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Exceptions;
using Telegram.BotKit.Extensions;

namespace Telegram.BotKit.Pipeline.Middlewares;

internal sealed class DefaultCommandErrorHandler(
    ITelegramBotClient bot,
    ILogger<DefaultCommandErrorHandler> logger
    ) : ICommandErrorHandlerMiddleware
{
    public async Task InvokeAsync(CommandContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        try
        {
            await next();
        }
        catch (MissingParameterException ex)
        {
            logger.LogWarning("Command validation failed: Missing required parameter '{ParamName}' in /{Command}", ex.ParameterName, context.Command);

            await TrySendMessageAsync(context,
                $"⚠️ Missing required parameter: <b>{ex.ParameterName}</b>",
                cancellationToken);
        }
        catch (InvalidParameterTypeException ex)
        {
            logger.LogWarning("Command validation failed: Parameter '{ParamName}' expects {TargetType}, got '{RawValue}' in /{Command}",
                ex.ParameterName, ex.TargetType.Name, ex.RawValue, context.Command);

            await TrySendMessageAsync(context,
                $"⚠️ Parameter <b>{ex.ParameterName}</b> must be of type <code>{ex.TargetType.Name}</code>.",
                cancellationToken);
        }
        catch (InputParsingException ex)
        {
            logger.LogWarning("Command parsing failed: {Message}. Input: {Input}", ex.Message, ex.Input);

            await TrySendMessageAsync(context,
                $"⚠️ Syntax error: {ex.Message}",
                cancellationToken);
        }
        catch (RouteNotFoundException)
        {
            logger.LogDebug("Unknown command received: /{Command}", context.Command);
            await TrySendMessageAsync(context, "🤷‍ Unknown command.", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error handling command /{Command}", context.Command);

            await TrySendMessageAsync(context,
                "🔥 An internal error occurred while processing your command.",
                cancellationToken);
        }
    }

    private async Task TrySendMessageAsync(CommandContext context, string text, CancellationToken ct)
    {
        try
        {
            await bot.ReplyMessage(context, text, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // если почему-то сообщение не отправилось, то просто залоггируем этот момент
            logger.LogWarning(ex, "Failed to send error response to chat {ChatId}", context.Message.Chat.Id);
        }
    }
}
