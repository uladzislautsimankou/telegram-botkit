using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.BotKit.ChosenInlineResults.Abstractions;
using Telegram.BotKit.ChosenInlineResults.Routing;
using Telegram.BotKit.Shared.Registration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers chosen inline result handlers in the registry and the dependency injection container.
/// </summary>
internal static class ChosenInlineResultRegistrationHelper
{
    /// <summary>
    /// Determines whether the type implements <see cref="IChosenInlineResultHandler"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements <see cref="IChosenInlineResultHandler"/>; otherwise, false.</returns>
    public static bool IsHandler(Type type) => typeof(IChosenInlineResultHandler).IsAssignableFrom(type);

    /// <summary>
    /// Registers a handler known only by its runtime type by continuing in the generic <see cref="RegisterHandler{THandler}"/>.
    /// </summary>
    /// <param name="services">The service collection the handler is registered in.</param>
    /// <param name="registry">The registry that stores the handler descriptors.</param>
    /// <param name="handlerType">The runtime type of the handler.</param>
    public static void RegisterHandlerByType(
        IServiceCollection services,
        ChosenInlineResultHandlerRegistry registry,
        Type handlerType) =>
        GenericMethodInvoker.Invoke(
            typeof(ChosenInlineResultRegistrationHelper),
            nameof(RegisterHandler),
            [handlerType],
            services,
            registry);

    /// <summary>
    /// Registers a handler: reads its static matching predicate and order and stores them in a descriptor.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <param name="services">The service collection the handler is registered in.</param>
    /// <param name="registry">The registry that stores the handler descriptors.</param>
    public static void RegisterHandler<THandler>(IServiceCollection services, ChosenInlineResultHandlerRegistry registry)
        where THandler : class, IChosenInlineResultHandler
    {
        services.TryAddTransient<THandler>();

        registry.Register(new ChosenInlineResultHandlerDescriptor(
            typeof(THandler),
            THandler.CanHandle,
            static sp => sp.GetRequiredService<THandler>(),
            THandler.Order));
    }
}

