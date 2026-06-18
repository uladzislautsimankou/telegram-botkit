namespace Telegram.BotKit.Invocation;

internal interface ICommandHandlerInvoker
{
    string Command { get; }
    
    string[] Aliases { get; }

    Type HandlerType { get; }

    object HandlerInstance { get; }

    Task InvokeAsync(CommandContext context, CancellationToken cancellationToken = default);
}
