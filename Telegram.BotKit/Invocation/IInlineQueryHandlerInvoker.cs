namespace Telegram.BotKit.Invocation;

internal interface IInlineQueryHandlerInvoker
{
    string Key { get; }

    Task InvokeAsync(InlineQueryContext context, CancellationToken cancellationToken = default);
}
