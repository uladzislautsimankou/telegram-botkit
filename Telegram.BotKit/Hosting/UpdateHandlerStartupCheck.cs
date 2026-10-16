using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Polling;
using Telegram.BotKit.Shared.Startup;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Checks that the application registered an <see cref="IUpdateHandler"/>, which both polling and webhook mode require.
/// </summary>
internal sealed class UpdateHandlerStartupCheck : IStartupCheck
{
    private const string Explanation =
        "The bot passes every incoming update to your IUpdateHandler; its HandleUpdateAsync routes the update " +
        "to the dispatchers (ICommandDispatcher, ICallbackDispatcher, IMessageDispatcher, " +
        "IInlineQueryDispatcher, IChosenInlineResultDispatcher). " +
        "Register it before Build(): builder.Services.AddSingleton<IUpdateHandler, YourUpdateHandler>();";

    /// <summary>
    /// The problem description for a service collection without any <see cref="IUpdateHandler"/> registration.
    /// </summary>
    public const string NotRegisteredMessage = "No IUpdateHandler is registered. " + Explanation;

    /// <summary>
    /// Determines whether an <see cref="IUpdateHandler"/> is registered. Works on the service collection, so it can run
    /// before the container is built: with validation on build (the Development environment) a missing handler would
    /// otherwise surface as a DI error about the polling service, not as a message about the handler.
    /// It does not tell whether the handler can be created; <see cref="Validate"/> checks that once the container exists.
    /// </summary>
    /// <param name="services">The service collection to inspect.</param>
    /// <returns>True if an <see cref="IUpdateHandler"/> registration exists; otherwise, false.</returns>
    public static bool IsRegistered(IServiceCollection services) =>
        services.Any(d => d.ServiceType == typeof(IUpdateHandler));

    /// <inheritdoc />
    public IEnumerable<string> Validate(IServiceProvider services)
    {
        try
        {
            // IServiceProviderIsService would not do here: it only looks for a registration and says nothing about
            // the constructor dependencies. Resolving the handler is the only way to find out that it can be created
            // (a singleton is created once and reused, so this does not create anything the bot would not create anyway).
            using var scope = services.CreateScope();

            if (scope.ServiceProvider.GetService<IUpdateHandler>() is null)
            {
                return [NotRegisteredMessage];
            }

            return [];
        }
        catch (InvalidOperationException ex)
        {
            // the container reports which dependency is missing
            return [$"IUpdateHandler is registered, but it cannot be created: {ex.Message}"];
        }
    }
}

