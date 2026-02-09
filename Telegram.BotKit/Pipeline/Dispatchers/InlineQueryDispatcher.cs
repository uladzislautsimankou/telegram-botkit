using Telegram.Bot.Types;
using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Pipeline.Middlewares;

namespace Telegram.BotKit.Pipeline.Dispatchers;

internal sealed class InlineQueryDispatcher(
    IInlineQueryErrorHandlerMiddleware errorHandler,
    IEnumerable<IInlineQueryMiddleware> userMiddlewares,
    InlineQueryRoutingMiddleware routerMiddleware
    ) : IInlineQueryDispatcher
{
    public async Task DispatchAsync(InlineQuery query, CancellationToken cancellationToken = default)
    {
        var context = new InlineQueryContext(query);

        // [Error Handler] -> [User Middleware 1] -> [User Middleware N] -> [Router]
        IInlineQueryMiddleware[] allSteps = [errorHandler, .. userMiddlewares, routerMiddleware];

        NextDelegate pipeline = () => Task.CompletedTask;

        foreach (var middleware in allSteps.Reverse())
        {
            var next = pipeline;
            pipeline = () => middleware.InvokeAsync(context, next, cancellationToken);
        }

        await pipeline();
    }
}
