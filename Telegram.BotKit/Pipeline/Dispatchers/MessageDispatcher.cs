using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Pipeline.Dispatchers;

internal sealed class MessageDispatcher(
    IEnumerable<IMessageMiddleware> userMiddlewares,
    IMessageErrorHandlerMiddleware errorHandler,
    IMessageStateMiddleware stateMiddleware,
    MessageRoutingMiddleware routerMiddleware,
    MessageExecutionMiddleware executionMiddleware
    ) : IMessageDispatcher
{
    public async Task DispatchAsync(Message message, CancellationToken cancellationToken = default)
    {
        var context = new MessageContext(message);

        // pipeline execution order is critical: handle errors, resolve current state, run routing, execute custom middlewares, and perform final handler execution
        IMessageMiddleware[] allSteps = [
            errorHandler,
            stateMiddleware,
            routerMiddleware,
            .. userMiddlewares,
            executionMiddleware
        ];

        NextDelegate pipeline = () => Task.CompletedTask;

        foreach (var middleware in allSteps.Reverse())
        {
            var next = pipeline;
            pipeline = () => middleware.InvokeAsync(context, next, cancellationToken);
        }

        await pipeline();
    }
}