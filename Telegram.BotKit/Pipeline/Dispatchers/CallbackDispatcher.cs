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

        // [Error Handler] -> [User Middleware 1] -> [User Middleware N] -> [Router]
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
