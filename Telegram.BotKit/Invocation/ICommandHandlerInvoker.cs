namespace Telegram.BotKit.Invocation;

internal interface ICommandHandlerInvoker
{
    string Command { get; }

    Task InvokeAsync(CommandContext context, CancellationToken cancellationToken = default);
}
