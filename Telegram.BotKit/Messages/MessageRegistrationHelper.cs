using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.BotKit.Messages.Abstractions;
using Telegram.BotKit.Messages.Routing;
using Telegram.BotKit.Shared.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers message handlers in the registry and the dependency injection container.
/// </summary>
internal static class MessageRegistrationHelper
{
    /// <summary>
    /// Determines whether the type implements <see cref="IMessageHandler"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements <see cref="IMessageHandler"/>; otherwise, false.</returns>
    public static bool IsHandler(Type type) => typeof(IMessageHandler).IsAssignableFrom(type);

    /// <summary>
    /// Registers a handler known only by its runtime type by continuing in the generic <see cref="RegisterHandler{THandler}"/>.
    /// </summary>
    /// <param name="services">The service collection the handler is registered in.</param>
    /// <param name="registry">The registry that stores the handler descriptors.</param>
    /// <param name="handlerType">The runtime type of the handler.</param>
    public static void RegisterHandlerByType(IServiceCollection services, MessageHandlerRegistry registry, Type handlerType) =>
        GenericMethodInvoker.Invoke(
            typeof(MessageRegistrationHelper),
            nameof(RegisterHandler),
            [handlerType],
            services,
            registry);

    /// <summary>
    /// Registers a handler: reads its static state, matching predicate and order, and stores them in a descriptor.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <param name="services">The service collection the handler is registered in.</param>
    /// <param name="registry">The registry that stores the handler descriptors, grouped by required user state.</param>
    public static void RegisterHandler<THandler>(IServiceCollection services, MessageHandlerRegistry registry)
        where THandler : class, IMessageHandler
    {
        services.TryAddTransient<THandler>();

        var requiredState = THandler.RequiredState?.Trim();
        if (string.IsNullOrWhiteSpace(requiredState))
        {
            requiredState = null;
        }

        registry.Register(new MessageHandlerDescriptor(
            typeof(THandler),
            THandler.CanHandle,
            static sp => sp.GetRequiredService<THandler>(),
            THandler.Order), requiredState);
    }
}

