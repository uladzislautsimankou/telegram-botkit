using Telegram.BotKit.Abstractions;

namespace Telegram.BotKit.Pipeline.Middlewares;

/// <summary>
/// A default demonstration of <see cref="IMessageStateMiddleware"/> illustrating how to fetch
/// and assign the user's conversational state before routing and handler execution begin.
/// </summary>
internal sealed class DefaultMessageStateMiddleware : IMessageStateMiddleware
{
    public async Task InvokeAsync(MessageContext context, NextDelegate next, CancellationToken cancellationToken = default)
    {
        // example: retrieve the telegram sender's id, fetch their current state from a database or storage,
        // and assign it to the context so that routing middleware can match state-specific handlers
        // var userId = context.Message.From?.Id;
        // if (userId is not null)
        // {
        //     context.UserState = await stateStorage.GetStateAsync(userId.Value);
        // }

        await next();
    }
}