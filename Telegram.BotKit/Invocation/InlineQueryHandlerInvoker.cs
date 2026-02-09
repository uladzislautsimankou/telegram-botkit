using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;

namespace Telegram.BotKit.Invocation;

internal sealed class InlineQueryHandlerInvoker<TParams>(
    IInlineQueryHandler<TParams> handler,
    IInlineQueryParameterBinder binder
    ) : IInlineQueryHandlerInvoker where TParams : class, new()
{
    public string Key => handler.Key;

    public async Task InvokeAsync(InlineQueryContext context, CancellationToken cancellationToken = default)
    {
        var parameters = binder.Bind<TParams>(context);
        await handler.HandleAsync(parameters, context, cancellationToken);
    }
}
