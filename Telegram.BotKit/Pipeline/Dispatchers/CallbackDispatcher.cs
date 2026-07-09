using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Pipeline.Dispatchers;

internal sealed class CallbackDispatcher(
    ICallbackErrorHandlerMiddleware errorHandler,
    IEnumerable<ICallbackMiddleware> userMiddlewares,
    CallbackRoutingMiddleware routerMiddleware
    ) : ICallbackDispatcher
{
    public async Task DispatchAsync(CallbackQuery query, CancellationToken cancellationToken = default)
    {
        var context = new CallbackContext(query);

        // pipeline execution order is critical: handle errors, execute custom middlewares, run routing
        ICallbackMiddleware[] allSteps = [errorHandler, .. userMiddlewares, routerMiddleware];

        NextDelegate pipeline = () => Task.CompletedTask;

        foreach (var middleware in allSteps.Reverse())
        {
            var next = pipeline;
            pipeline = () => middleware.InvokeAsync(context, next, cancellationToken);
        }

        await pipeline();
    }
}
