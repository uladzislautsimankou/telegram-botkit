using Telegram.BotKit.Commands.Models;
using Telegram.BotKit.Shared.Binding;

namespace Telegram.BotKit.Commands.Binding;

/// <summary>
/// Defines a binder responsible for mapping incoming command arguments (e.g., positional or key=value arguments) to a strongly typed model.
/// </summary>
internal interface ICommandParameterBinder : IParameterModelValidator
{
    /// <summary>
    /// Binds command arguments from the context to a new instance of <typeparamref name="TParams"/>.
    /// </summary>
    /// <typeparam name="TParams">The type of the parameter model to bind to.</typeparam>
    /// <param name="context">The command execution context.</param>
    /// <returns>A populated instance of the parameter model.</returns>
    TParams Bind<TParams>(CommandContext context) where TParams : class, new();
}
