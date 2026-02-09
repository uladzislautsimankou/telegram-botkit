namespace Telegram.BotKit.Abstractions;

/// <summary>
/// Provides context information for value conversion.
/// </summary>
/// <param name="Input">The raw string input to convert.</param>
/// <param name="TargetType">The type of the property being bound.</param>
/// <param name="Services">The service provider for resolving dependencies.</param>
/// <param name="TargetObject">The instance of the DTO object being populated. Useful for cross-property dependencies.</param>
public record ValueConversionContext(
    string Input,
    Type TargetType,
    object TargetObject,
    IServiceProvider? Services = null
);
