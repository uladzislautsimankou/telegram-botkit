using Telegram.BotKit.Abstractions;
using Telegram.BotKit.Binding.Binders;

namespace Telegram.BotKit.Invocation;

internal sealed class CommandHandlerInvoker<TParams>(
    ICommandHandler<TParams> handler,
    ICommandParameterBinder binder
    ) : ICommandHandlerInvoker where TParams : class, new()
{
    public string Command => handler.Command;

    public string[] Aliases => handler.Aliases;

    public async Task InvokeAsync(CommandContext context, CancellationToken cancellationToken = default)
    {
        var parameters = binder.Bind<TParams>(context);
        await handler.HandleAsync(parameters, context, cancellationToken);
    }
}
