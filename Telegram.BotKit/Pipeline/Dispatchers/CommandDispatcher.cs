using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Pipeline.Dispatchers;

internal sealed class CommandDispatcher(
    ICommandErrorHandlerMiddleware errorHandler,
    IEnumerable<ICommandMiddleware> userMiddlewares,
    CommandRoutingMiddleware routerMiddleware
    ) : ICommandDispatcher
{
    public async Task DispatchAsync(Message message, CancellationToken cancellationToken = default)
    {
        var context = new CommandContext(message);

        // [Error Handler] -> [User Middleware 1] -> [User Middleware N] -> [Router]
        ICommandMiddleware[] allSteps = [errorHandler, .. userMiddlewares, routerMiddleware];

        NextDelegate pipeline = () => Task.CompletedTask;

        foreach (var middleware in allSteps.Reverse())
        {
            var next = pipeline;
            pipeline = () => middleware.InvokeAsync(context, next, cancellationToken);
        }

        await pipeline();
    }
}

