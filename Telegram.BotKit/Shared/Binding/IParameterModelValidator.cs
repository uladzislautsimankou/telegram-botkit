namespace Telegram.BotKit.Shared.Binding;

/// <summary>
/// Checks parameter models (command arguments, callback data) for mistakes without binding any input.
/// </summary>
internal interface IParameterModelValidator
{
    /// <summary>
    /// Checks that a parameter model can be bound: parameter names and positions are unique
    /// and every property type has a value converter.
    /// </summary>
    /// <param name="modelType">The parameter model type.</param>
    /// <returns>A description of every problem found; empty when the model is valid.</returns>
    IReadOnlyList<string> Validate(Type modelType);
}
