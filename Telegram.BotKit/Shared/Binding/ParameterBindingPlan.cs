using System.Collections.Frozen;

namespace Telegram.BotKit.Shared.Binding;

/// <summary>
/// Precomputed binding information for a parameter model type.
/// </summary>
/// <param name="Params">Bindable properties: named-only first, then positional ones by ascending position.</param>
/// <param name="NamedKeys">Names that may be supplied in the "name=value" form.</param>
internal sealed record ParameterBindingPlan(ParamMetadata[] Params, FrozenSet<string> NamedKeys);
