using Telegram.BotKit.Commands.Abstractions;
using Telegram.BotKit.Commands.Binding;
using Telegram.BotKit.Commands.Models;

namespace Telegram.BotKit.Commands.Invocation;

/// <summary>
/// Standard generic invoker that binds incoming parameters and routes execution to the target command handler.
/// </summary>
internal sealed class CommandHandlerInvoker<TParams>(
    ICommandHandler<TParams> handler,
    ICommandParameterBinder binder
    ) : ICommandHandlerInvoker where TParams : class, new()
{
    public Type HandlerType => typeof(ICommandHandler<TParams>);

    public object HandlerInstance => handler;

    public async Task InvokeAsync(CommandContext context, CancellationToken cancellationToken = default)
    {
        var parameters = binder.Bind<TParams>(context);
        await handler.HandleAsync(parameters, context, cancellationToken);
    }
}
