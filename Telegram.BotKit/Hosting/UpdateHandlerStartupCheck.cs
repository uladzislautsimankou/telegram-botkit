using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Polling;
using Telegram.BotKit.Shared.Startup;

namespace Telegram.BotKit.Hosting;

/// <summary>
/// Checks that the application registered an <see cref="IUpdateHandler"/> that can be created. Both polling and webhook
/// mode require it, but only the polling service asks the container for it when the host starts; the webhook endpoint
/// would fail with the first update, so the check makes both modes report the problem when the application is built.
/// </summary>
internal sealed class UpdateHandlerStartupCheck : IStartupCheck
{
    private const string Explanation =
        "The bot passes every incoming update to your IUpdateHandler; its HandleUpdateAsync routes the update " +
        "to the dispatchers (ICommandDispatcher, ICallbackDispatcher, IMessageDispatcher, " +
        "IInlineQueryDispatcher, IChosenInlineResultDispatcher). " +
        "Register it before Build(): builder.Services.AddSingleton<IUpdateHandler, YourUpdateHandler>();";

    private const string NotRegisteredMessage = "No IUpdateHandler is registered. " + Explanation;

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
        catch (Exception ex)
        {
            // the container reports which dependency is missing (the exception type depends on the container)
            return [$"IUpdateHandler is registered, but it cannot be created: {ex.Message}"];
        }
    }
}
