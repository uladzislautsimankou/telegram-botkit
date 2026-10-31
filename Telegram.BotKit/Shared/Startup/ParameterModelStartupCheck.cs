using Telegram.BotKit.Shared.Binding;

namespace Telegram.BotKit.Shared.Startup;

/// <summary>
/// Validates the parameter models of all registered handlers of one kind (commands or callbacks).
/// </summary>
/// <param name="handlerKind">The kind of handlers used in messages, e.g. "Command".</param>
/// <param name="getModels">Returns the handler type and the parameter model type of every registered handler.</param>
/// <param name="getValidator">Resolves the binder that validates the models.</param>
internal sealed class ParameterModelStartupCheck(
    string handlerKind,
    Func<IEnumerable<(Type HandlerType, Type ParamsType)>> getModels,
    Func<IServiceProvider, IParameterModelValidator> getValidator) : IStartupCheck
{
    /// <inheritdoc />
    public IEnumerable<string> Validate(IServiceProvider services)
    {
        var validator = getValidator(services);
        var problems = new List<string>();

        foreach (var (handlerType, paramsType) in getModels())
        {
            foreach (var problem in validator.Validate(paramsType))
            {
                problems.Add($"{handlerKind} handler '{handlerType.Name}': {problem}");
            }
        }

        return problems;
    }
}

