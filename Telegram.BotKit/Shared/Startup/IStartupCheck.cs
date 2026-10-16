namespace Telegram.BotKit.Shared.Startup;

/// <summary>
/// A check of the application setup that runs once when the bot application is built,
/// before anything connects to Telegram.
/// </summary>
internal interface IStartupCheck
{
    /// <summary>
    /// Inspects the built service provider.
    /// </summary>
    /// <param name="services">The root service provider of the built application.</param>
    /// <returns>A description of every problem found; empty when the setup is valid.</returns>
    IEnumerable<string> Validate(IServiceProvider services);
}
